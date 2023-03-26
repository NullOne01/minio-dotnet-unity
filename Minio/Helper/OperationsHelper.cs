/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Minio.DataModel;
using Minio.Exceptions;
using Minio.Helper;
using UnityEngine;

namespace Minio
{
    public partial class MinioClient : IObjectOperations
    {
        /// <summary>
        ///     private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task<ObjectStat> getObjectHelper(GetObjectArgs args,
            CancellationToken cancellationToken = default)
        {
            Debug.Log("getObjectHelper");
            // StatObject is called to both verify the existence of the object and return it with GetObject.
            // NOTE: This avoids writing the error body to the action stream passed (Do not remove).

            var statArgs = new StatObjectArgs()
                .WithBucket(args.BucketName)
                .WithObject(args.ObjectName)
                .WithVersionId(args.VersionId)
                .WithMatchETag(args.MatchETag)
                .WithNotMatchETag(args.NotMatchETag)
                .WithModifiedSince(args.ModifiedSince)
                .WithUnModifiedSince(args.UnModifiedSince)
                .WithServerSideEncryption(args.SSE)
                .WithHeaders(args.Headers);
            if (args.OffsetLengthSet) statArgs.WithOffsetAndLength(args.ObjectOffset, args.ObjectLength);
            var objStat = await StatObjectAsync(statArgs, cancellationToken);
            Debug.Log("args?.Validate(); if (args.FileName != null)");
            args?.Validate();
            if (args.FileName != null)
                await getObjectFileAsync(args, objStat, cancellationToken);
            else if (args.CallBack is not null)
                await getObjectStreamAsync(args, objStat, args.CallBack, cancellationToken);
            else await getObjectStreamAsync(args, objStat, args.FuncCallBack, cancellationToken);
            return objStat;
        }

        /// <summary>
        ///     private helper method return the specified object from the bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="objectStat"> ObjectStat object encapsulates information like - object name, size, etag etc </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private Task getObjectFileAsync(GetObjectArgs args, ObjectStat objectStat,
            CancellationToken cancellationToken = default)
        {
            Debug.Log("getObjectFileAsync");
            var length = objectStat.Size;
            var etag = objectStat.ETag;

            var tempFileName = $"{args.FileName}.{etag}.part.minio";
            if (!string.IsNullOrEmpty(args.VersionId))
                tempFileName = $"{args.FileName}.{etag}.{args.VersionId}.part.minio";
            if (File.Exists(args.FileName)) File.Delete(args.FileName);

            Utils.ValidateFile(tempFileName);
            if (File.Exists(tempFileName)) File.Delete(tempFileName);

            Func<Stream, CancellationToken, Task> callbackAsync =
                async (Stream stream, CancellationToken cancellationToken) =>
                {
                    using var dest = new FileStream(tempFileName, FileMode.Create, FileAccess.Write);
                    Debug.Log("await stream.CopyToAsync(dest, cancellationToken)");
                    // TODO(nullone): CopyToAsync was used here before, but WebGL doesn't support it :/
                    stream.CopyTo(dest);
                    Debug.Log("AFTER: await stream.CopyToAsync(dest, cancellationToken)");
                };

#pragma warning disable IDISP001 // Dispose created
            var cts = new CancellationTokenSource();
#pragma warning restore IDISP001 // Dispose created
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            args.WithCallbackStream(async (stream, cancellationToken) =>
            {
                Debug.Log("await callbackAsync(stream, cts.Token);");
                await callbackAsync(stream, cts.Token);
                Debug.Log("Utils.MoveWithReplace(tempFileName, args.FileName);");
                Utils.MoveWithReplace(tempFileName, args.FileName);
            });
            return getObjectStreamAsync(args, objectStat, null, cancellationToken);
        }

        /// <summary>
        ///     private helper method. It returns the specified portion or full object from the bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="objectStat">
        ///     ObjectStat object encapsulates information like - object name, size, etag etc, represents
        ///     Object Information
        /// </param>
        /// <param name="cb"> Action object of type Stream, callback to send Object contents, if assigned </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task getObjectStreamAsync(GetObjectArgs args, ObjectStat objectStat, Action<Stream> cb,
            CancellationToken cancellationToken = default)
        {
            Debug.Log("getObjectStreamAsync");
            var requestMessageBuilder = await CreateRequest(args);
            using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
                ;
        }

        /// <summary>
        ///     private helper method. It returns the specified portion or full object from the bucket
        /// </summary>
        /// <param name="args">GetObjectArgs Arguments Object encapsulates information like - bucket name, object name etc </param>
        /// <param name="objectStat">
        ///     ObjectStat object encapsulates information like - object name, size, etag etc, represents
        ///     Object Information
        /// </param>
        /// <param name="cb">
        ///     Callback function to send/process Object contents using
        ///     async Func object which takes Stream and CancellationToken as input
        ///     and Task as output, if assigned
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        private async Task getObjectStreamAsync(GetObjectArgs args, ObjectStat objectStat,
            Func<Stream, CancellationToken, Task> cb,
            CancellationToken cancellationToken = default)
        {
            Debug.Log("var requestMessageBuilder = await CreateRequest(args);");
            var requestMessageBuilder = await CreateRequest(args);
            Debug.Log("using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);");
            using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken);
        }

        /// <summary>
        ///     private helper method to remove list of objects from bucket
        /// </summary>
        /// <param name="args">
        ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
        ///     optional list of versions (for each object) to be deleted
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        private async Task<IList<DeleteError>> removeObjectsAsync(RemoveObjectsArgs args,
            CancellationToken cancellationToken)
        {
            var requestMessageBuilder = await CreateRequest(args);
            using var response = await ExecuteTaskAsync(NoErrorHandlers, requestMessageBuilder, cancellationToken)
                ;
            var removeObjectsResponse = new RemoveObjectsResponse(response.StatusCode, response.Content);
            return removeObjectsResponse.DeletedObjectsResult.errorList;
        }

        /// <summary>
        ///     private helper method to call remove objects function
        /// </summary>
        /// <param name="args">
        ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
        ///     optional version Id list
        /// </param>
        /// <param name="objVersions">List of Tuples. Each tuple is Object name to List of Version IDs to be deleted</param>
        /// <param name="fullErrorsList">
        ///     Full List of DeleteError objects. The error list from this call will be added to the full
        ///     list.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<IList<DeleteError>> callRemoveObjectVersions(RemoveObjectsArgs args,
            IList<Tuple<string, string>> objVersions, List<DeleteError> fullErrorsList,
            CancellationToken cancellationToken)
        {
            var iterArgs = new RemoveObjectsArgs()
                .WithBucket(args.BucketName)
                .WithObjectsVersions(objVersions);
            var errorsList = await removeObjectsAsync(iterArgs, cancellationToken);
            fullErrorsList.AddRange(errorsList);
            return fullErrorsList;
        }

        /// <summary>
        ///     private helper method to call function to remove objects/version items in iterations of 1000 each from bucket
        /// </summary>
        /// <param name="args">
        ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
        ///     optional list of versions (for each object) to be deleted
        /// </param>
        /// <param name="objNames">List of Object names to be deleted</param>
        /// <param name="fullErrorsList">
        ///     Full List of DeleteError objects. The error list from this call will be added to the full
        ///     list.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        private async Task<IList<DeleteError>> callRemoveObjects(RemoveObjectsArgs args, IList<string> objNames,
            List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            // var requestMessageBuilder = await this.CreateRequest(args);
            var iterArgs = new RemoveObjectsArgs()
                .WithBucket(args.BucketName)
                .WithObjects(objNames);
            var errorsList = await removeObjectsAsync(iterArgs, cancellationToken);
            fullErrorsList.AddRange(errorsList);
            return fullErrorsList;
        }

        /// <summary>
        ///     private helper method to remove objects/version items in iterations of 1000 each from bucket
        /// </summary>
        /// <param name="args">
        ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
        ///     optional list of versions (for each object) to be deleted
        /// </param>
        /// <param name="fullErrorsList">
        ///     Full List of DeleteError objects. The error list from this call will be added to the full
        ///     list.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        private async Task<IList<DeleteError>> removeObjectVersionsHelper(RemoveObjectsArgs args,
            List<DeleteError> fullErrorsList, CancellationToken cancellationToken)
        {
            if (args.ObjectNamesVersions.Count <= 1000)
            {
                fullErrorsList.AddRange(await callRemoveObjectVersions(args, args.ObjectNamesVersions, fullErrorsList,
                    cancellationToken));
                return fullErrorsList;
            }

            var curItemList = new List<Tuple<string, string>>(args.ObjectNamesVersions.GetRange(0, 1000));
            var delVersionNextIndex = curItemList.Count;
            var deletedCount = 0;
            while (delVersionNextIndex <= args.ObjectNamesVersions.Count)
            {
                var errorList = await callRemoveObjectVersions(args, curItemList, fullErrorsList, cancellationToken)
                    ;
                if (delVersionNextIndex == args.ObjectNamesVersions.Count)
                    break;
                deletedCount += curItemList.Count;
                fullErrorsList.AddRange(errorList);
                curItemList.Clear();
                if (args.ObjectNamesVersions.Count - delVersionNextIndex <= 1000)
                {
                    curItemList.AddRange(args.ObjectNamesVersions.GetRange(delVersionNextIndex,
                        args.ObjectNamesVersions.Count - delVersionNextIndex));
                    delVersionNextIndex = args.ObjectNamesVersions.Count;
                }
                else
                {
                    curItemList.AddRange(args.ObjectNamesVersions.GetRange(delVersionNextIndex, 1000));
                    delVersionNextIndex += 1000;
                }
            }

            return fullErrorsList;
        }

        /// <summary>
        ///     private helper method to remove objects in iterations of 1000 each from bucket
        /// </summary>
        /// <param name="args">
        ///     RemoveObjectsArgs Arguments Object encapsulates information like - bucket name, List of objects,
        ///     optional list of versions (for each object) to be deleted
        /// </param>
        /// <param name="fullErrorsList">
        ///     Full List of DeleteError objects. The error list from this call will be added to the full
        ///     list.
        /// </param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="AuthorizationException">When access or secret key provided is invalid</exception>
        /// <exception cref="InvalidBucketNameException">When bucket name is invalid</exception>
        /// <exception cref="InvalidObjectNameException">When object name is invalid</exception>
        /// <exception cref="BucketNotFoundException">When bucket is not found</exception>
        /// <exception cref="ObjectNotFoundException">When object is not found</exception>
        /// <exception cref="MalFormedXMLException">When configuration XML provided is invalid</exception>
        private async Task<IList<DeleteError>> removeObjectsHelper(RemoveObjectsArgs args,
            IList<DeleteError> fullErrorsList,
            CancellationToken cancellationToken)
        {
            var iterObjects = new List<string>(1000);
            var i = 0;
            foreach (var objName in args.ObjectNames)
            {
                Utils.ValidateObjectName(objName);
                iterObjects.Insert(i, objName);
                if (++i == 1000)
                {
                    fullErrorsList =
                        await callRemoveObjects(args, iterObjects, fullErrorsList.ToList(), cancellationToken)
                            ;
                    iterObjects.Clear();
                    i = 0;
                }
            }

            if (iterObjects.Count > 0)
                fullErrorsList = await callRemoveObjects(args, iterObjects, fullErrorsList.ToList(), cancellationToken)
                    ;
            return fullErrorsList;
        }
    }

    public static class OperationsUtil
    {
        private static readonly List<string> SupportedHeaders = new()
        {
            "cache-control", "content-encoding", "content-type",
            "x-amz-acl", "content-disposition", "x-minio-extract"
        };

        private static readonly List<string> SSEHeaders = new()
        {
            "X-Amz-Server-Side-Encryption-Customer-Algorithm",
            "X-Amz-Server-Side-Encryption-Customer-Key",
            "X-Amz-Server-Side-Encryption-Customer-Key-Md5",
            Constants.SSEGenericHeader,
            Constants.SSEKMSKeyId,
            Constants.SSEKMSContext
        };

        internal static bool IsSupportedHeader(string hdr, IEqualityComparer<string> comparer = null)
        {
            comparer ??= StringComparer.OrdinalIgnoreCase;
            return SupportedHeaders.Contains(hdr, comparer);
        }

        internal static bool IsSSEHeader(string hdr, IEqualityComparer<string> comparer = null)
        {
            comparer ??= StringComparer.OrdinalIgnoreCase;
            return SSEHeaders.Contains(hdr, comparer);
        }
    }
}