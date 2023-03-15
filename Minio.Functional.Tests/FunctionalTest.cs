/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage,
* (C) 2017-2021 MinIO, Inc.
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

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;
using Minio.DataModel.ILM;
using Minio.DataModel.ObjectLock;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace Minio.Functional.Tests
{
    public static class FunctionalTest
    {
        private const int KB = 1024;
        private const int MB = 1024 * 1024;
        private const int GB = 1024 * 1024 * 1024;

        private const string dataFile1B = "datafile-1-b";

        private const string dataFile10KB = "datafile-10-kB";
        private const string dataFile6MB = "datafile-6-MB";

        private const string makeBucketSignature =
            "Task MakeBucketAsync(string bucketName, string location = 'us-east-1', CancellationToken cancellationToken = default(CancellationToken))";

        private const string listBucketsSignature =
            "Task<ListAllMyBucketsResult> ListBucketsAsync(CancellationToken cancellationToken = default(CancellationToken))";

        private const string bucketExistsSignature =
            "Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeBucketSignature =
            "Task RemoveBucketAsync(string bucketName, CancellationToken cancellationToken = default(CancellationToken))";

        private const string listObjectsSignature =
            "IObservable<Item> ListObjectsAsync(string bucketName, string prefix = null, bool recursive = false, CancellationToken cancellationToken = default(CancellationToken))";

        private const string putObjectSignature =
            "Task PutObjectAsync(PutObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getObjectSignature =
            "Task GetObjectAsync(GetObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string listIncompleteUploadsSignature =
            "IObservable<Upload> ListIncompleteUploads(ListIncompleteUploads args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string listenBucketNotificationsSignature =
            "IObservable<MinioNotificationRaw> ListenBucketNotificationsAsync(ListenBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string copyObjectSignature =
            "Task<CopyObjectResult> CopyObjectAsync(CopyObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string statObjectSignature =
            "Task<ObjectStat> StatObjectAsync(StatObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeObjectSignature1 =
            "Task RemoveObjectAsync(RemoveObjectArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeObjectSignature2 =
            "Task<IObservable<DeleteError>> RemoveObjectsAsync(RemoveObjectsArgs, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeIncompleteUploadSignature =
            "Task RemoveIncompleteUploadAsync(RemoveIncompleteUploadArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string presignedPutObjectSignature =
            "Task<string> PresignedPutObjectAsync(PresignedPutObjectArgs args)";

        private const string presignedGetObjectSignature =
            "Task<string> PresignedGetObjectAsync(PresignedGetObjectArgs args)";

        private const string presignedPostPolicySignature =
            "Task<Dictionary<string, string>> PresignedPostPolicyAsync(PresignedPostPolicyArgs args)";

        private const string getBucketPolicySignature =
            "Task<string> GetPolicyAsync(GetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setBucketPolicySignature =
            "Task SetPolicyAsync(SetPolicyArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getBucketNotificationSignature =
            "Task<BucketNotification> GetBucketNotificationAsync(GetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setBucketNotificationSignature =
            "Task SetBucketNotificationAsync(SetBucketNotificationsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeAllBucketsNotificationSignature =
            "Task RemoveAllBucketNotificationsAsync(RemoveAllBucketNotifications args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setBucketEncryptionSignature =
            "Task SetBucketEncryptionAsync(SetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getBucketEncryptionSignature =
            "Task<ServerSideEncryptionConfiguration> GetBucketEncryptionAsync(GetBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeBucketEncryptionSignature =
            "Task RemoveBucketEncryptionAsync(RemoveBucketEncryptionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string selectObjectSignature =
            "Task<SelectResponseStream> SelectObjectContentAsync(SelectObjectContentArgs args,CancellationToken cancellationToken = default(CancellationToken))";

        private const string setObjectLegalHoldSignature =
            "Task SetObjectLegalHoldAsync(SetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getObjectLegalHoldSignature =
            "Task<bool> GetObjectLegalHoldAsync(GetObjectLegalHoldArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setObjectLockConfigurationSignature =
            "Task SetObjectLockConfigurationAsync(SetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getObjectLockConfigurationSignature =
            "Task<ObjectLockConfiguration> GetObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string deleteObjectLockConfigurationSignature =
            "Task RemoveObjectLockConfigurationAsync(GetObjectLockConfigurationArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getBucketTagsSignature =
            "Task<Tagging> GetBucketTagsAsync(GetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setBucketTagsSignature =
            "Task SetBucketTagsAsync(SetBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string deleteBucketTagsSignature =
            "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setVersioningSignature =
            "Task SetVersioningAsync(SetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getVersioningSignature =
            "Task<VersioningConfiguration> GetVersioningAsync(GetVersioningArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string removeVersioningSignature =
            "Task RemoveBucketTagsAsync(RemoveBucketTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getObjectTagsSignature =
            "Task<Tagging> GetObjectTagsAsync(GetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setObjectTagsSignature =
            "Task SetObjectTagsAsync(SetObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string deleteObjectTagsSignature =
            "Task RemoveObjectTagsAsync(RemoveObjectTagsArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setObjectRetentionSignature =
            "Task SetObjectRetentionAsync(SetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getObjectRetentionSignature =
            "Task<ObjectRetentionConfiguration> GetObjectRetentionAsync(GetObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string clearObjectRetentionSignature =
            "Task ClearObjectRetentionAsync(ClearObjectRetentionArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string getBucketLifecycleSignature =
            "Task<LifecycleConfiguration> GetBucketLifecycleAsync(GetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string setBucketLifecycleSignature =
            "Task SetBucketLifecycleAsync(SetBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private const string deleteBucketLifecycleSignature =
            "Task RemoveBucketLifecycleAsync(RemoveBucketLifecycleArgs args, CancellationToken cancellationToken = default(CancellationToken))";

        private static readonly Random rnd = new();

        private static readonly RandomStreamGenerator rsg = new(100 * MB);

        private static string Bash(string cmd)
    {
        
        var Replacements = new Dictionary<string, string>
        {
            { "$", "\\$" }, { "(", "\\(" },
            { ")", "\\)" }, { "{", "\\{" },
            { "}", "\\}" }, { "[", "\\[" },
            { "]", "\\]" }, { "@", "\\@" },
            { "%", "\\%" }, { "&", "\\&" },
            { "#", "\\#" }, { "+", "\\+" }
        };
        foreach (var toReplace in Replacements.Keys) cmd = cmd.Replace(toReplace, Replacements[toReplace]);
        var cmdNoReturn = cmd + " >/dev/null 2>&1";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{cmdNoReturn}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        var result = process.StandardOutput.ReadLine();
        process.WaitForExit();
        return result;
    }

        // Create a file of given size from random byte array or optionally create a symbolic link
        // to the dataFileName residing in MINT_DATA_DIR
        private static string CreateFile(int size, string dataFileName = null)
    {

                var fileName = GetRandomName();

       if (!IsMintEnv())
        {
            var data = new byte[size];
            rnd.NextBytes(data);

            File.WriteAllBytes(fileName, data);
            return GetFilePath(fileName);
        }

        return GetFilePath(dataFileName);
    }

        public static string GetRandomObjectName(int length = 5)
    {
 
               // Server side does not allow the following characters in object names
        // '-', '_', '.', '/', '*'
        var characters = "abcd+%$#@&{}[]()";
        var result = new StringBuilder(length);

       for (var i = 0; i < length; i++) result.Append(characters[rnd.Next(characters.Length)]);
        return result.ToString();
    }

        // Generate a random string
        public static string GetRandomName(int length = 5)
    {
  
              var characters = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (length > 50) length = 50;

       var result = new StringBuilder(length);
        for (var i = 0; i < length; i++) result.Append(characters[rnd.Next(characters.Length)]);

       return "minio-dotnet-example-" + result;
    }

        internal static void GenerateRandomFile(string fileName)
    {
             using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        var fileSize = 3L * 1024 * 1024 * 1024;
        var segments = fileSize / 10000;
        var last_seg = fileSize % 10000;
        using var br = new BinaryWriter(fs);

       for (long i = 0; i < segments; i++)
            br.Write(new byte[10000]);

       br.Write(new byte[last_seg]);
        br.Close();
    }

        // Return true if running in Mint mode
        public static bool IsMintEnv()
        {
            return !stri
        g
        .IsNul
        l
        OrEmpty(Envir
        o
        nment.GetEn
        v
        ironmentVariable("MINT
        _
        DATA_DIR"));
  
         
         
        }

        // Get full path of file
        public static string GetFilePath(string fileName)
        {
  
             var
         d        ir = Environm
        nt.GetE
        v
        ronmentVari
        a
        ble("MINT_DATA_DIR");

         
               if (!str
        i
        ng.IsNullOrE
        p
        t
        y(data
        D
        ir)) return $
        "
        {dataDi
        r
        }
        {fileN
        me}
        ";

   
           
          var pa
        th
          Directory.G
        tCur
        e
        tDirector
        y
        ();
        return 
        $
        "
        {path}/{fileName
        ";

            
        }

        internal static void RunCoreTests(MinioClient minioClient)
        {

              
        // C
        eck if bucke
        t
         exists
   
            BucketE
        xi        Test(minioClient).Wait();

        // Create a new bucket
   
         
            MakeBuc
        k
        e
        t_Te
        s
        t
        1(minioClient).Wait();
        PutObject_Test1(minioClient
        )
        .Wait();
  
         
         
            
        P
        u
        tObject_Test2(minioClient
        )
        .Wait();
  
         
         
            
        L
        i
        stObjects_Test1(minioClie
        n
        t).Wait();

         
         
            
         
         
        RemoveObject_Test1(minioCli
        e
        nt).Wait();
        

         
            
         
         
         CopyObject_Test1(minioClien
        t
        ).Wait();


         
         
            
         
         
        // Test SetPolicyAsync fun
        c
        tion
      
         
         
        SetB
        u
        c
        ketPolicy_Test1(minioClient).Wait();

        // Test Presigned Get/Put 
        o
        perations
 
         
         
            
         
        P
        resignedGetObject_Test1(minioClient).Wait();
        PresignedPutObject_Test1(mi
        n
        ioClient).W
        a
        i
        t();
        

        

                // Test incomplete uploads
        

                Lis
        t
        I
        ncom
        p
        l
        eteUpload_Test1(minioClient).Wait();
        RemoveIncompleteUpload_Test
        (
        minioClient
        )
        .
        Wait
        (
        )
        ;

        // Test GetBucket policy
 
         
              GetBu
        c
        k
        etPo
        l
        i
        cy_Test1(minioClient).Wait();
    }

        internal static async Task BucketExists_Test(MinioClient minio)
        {
  
             var 
        tartTi
        me.Now        ;
                                v        ar         bucketN
        a
        me = GetRan
        d
        o
        mNam
        e
        (
        );                  r mbArgs
        = new 
        akeBu
        ketA
        gs()
            
        .
        WithBucket(
        ucket
        Na        
        var 
        eArgs = n
        w
        BucketEx
        i
        sts
        Args()
      
             .With
        u
        ket(bucketNam
        e
        )
        ;
        var
        rbArgs
        =
        new
        RemoveBucketAr
        g
        s(         
         .WithBuck
        e
        t(bucketNa
        m
        e);
        v
        r args
        =
        new
        Dictionary<strin
        g
        ,          
           {
     
         
              { "b
        u
        cketName", bu
        ketNam
         
        
  
             };

       
         
        tr         
                  
         
        await mini
        o
        .MakeBucketAs
        nc(m
        A
        gs)
        ConfigureA
        w
        ait(fa
        l
        e);
  
                  va        a
        t minio.Buck
        e
        ExistsAsyn
        (beArgs).Co
        nigureAwait(false);
            rt.Is
        rue(f
        o
        und);
         
         
          new 
        M
        i
        ntLogger(nameo
        f
        (Buck
        e
        tE         bu
        ketEx
        s
        sSign
        ture,
         
        "Tests whether Bu
        c
        ketExi
        s
        t
        s passes",
   
         
             
         
                  us.PAS
        S
        , Date
        T
        ime.N
        o
        w          ar
        s: args).L
        o
        g();
 
         
              }
        c
        a
        t
        h (NotImplementedExce
        p
        ion ex)
        {
            new M
        in        ucketExist
        s
        _Tes
        t
        , bucket
        E
        xis
        s
        ignature,
         
        Test
        s
        whet
        h
        e
        r B
        u
        c
        ketExists passes",
      
         
                TestStatus.NA, 
        at
        eTime.Now -         x.M
        ssage, ex.
        T
        oStrin
        g
        (), args: args).L
        o
        g
        );
        }
        
        c
        tch (Exception ex)
        {
      
                  er(nameof(
        B
        uc
        k
        tExists_
        T
        est
        ,
        bucketExi
        s
        sS
        i
        gnature
        ,
        "T
        e
        sts whet
        h
        e
        r
        Buck
        e
        Exis
        t
        s
         pa
        s
        s
        es",
                Test
        t
        atus.FAIL
         D
        ateTime.Now          ex
        Message, e
        x
        .ToStr
        i
        ng(), args: args)
        .
        L
        g();
            thro
        w
        
        }
        finally
        
        {
        t minio.Re
        m
        oveB
        u
        ketAsync
        (
        rbA
        g
        ).Configu
        r
        Aw
        a
        it(fals
        e
        ;

         
               }
        

         
         
         }

        internal static async Task RemoveBucket_Test1(MinioClient minio)
        {
                                       va        r 
        sta        e.Now        
             
         
          var        uc        ketName
         
        = G
        t
        andomName
        (
        0);               
         
        ar         m        b
        Args =         n        ew
         
        M
        a
        eBuc
        k
        tArg
        s(        )
                   
         
         
                  ket(b        u
                var beArg
        ck        ()
  
             
         
                   .W        ithBu        ck        t(bucketN        am        e);
                   
         
          var rb        Ar        s =         n        ew
         
        Remov        eB        c
         
         W        ucket(bu
        ketNam
        );
  
            
        var args = new Dic
        t
        ionary<stri
        g, st
        ri                {
   
                {
        "
        ucketNam
        e
        ", 
        bucketName }

               };

         
              var fou
        n
        d 
        =
         false;
     
          try

         
           
         {
           
         
        aw        k
        eBucketAsy
        n
        c(mbArgs).
        C
        onfigureAwait
        false)
        

           
               found = a
        w
        ai        e
        tExistsAsy
        n
        c(beArgs).
        C
        onfigureAwait
        false)
        

           
               Assert.Is
        T
        ru         
                 a
        w
        ait minio.
        R
        emoveBucketAs
        nc(r
        A
        gs)
        ConfigureA
        w
        ait(fa
        l
        e);
  
                  fo        m
        nio.BucketEx
        i
        tsAsync(be
        rgs).Config
        ueAwait(false
        ;
   
         
             
         Assert.IsFalse(found);
        new M
        ntLog
        g
        er(nameof(Remov
        e
        Bucket
        _
        T
        est1), removeB
        u
        cketS
        i
        gn        s whe
        h
        r Rem
        veBuc
        k
        et passes",
     
         
              
         
         
          TestStatus.P
        A
        SS, D
        a
        te        tartTi
        m
        e, arg
        s
        : arg
        s
        ).           }

             
         
         catch (Exception
         
        ex)
  
         
         
            {
        
         
           ne
        w
         M        meof(
        e
        oveBu
        ket_T
        e
        st1), removeBucke
        t
        Signat
        u
        r
        e, "Tests whet
        h
        er Re
        m
        ov        es",
 
         
               
         
             
         
        Te        L, 
        ateTime.No
        w
         - sta
        r
        tTime, ex.Message,
         
        e
        .ToString(), args: ar
        g
        ).Log();
            throw;
       
         }        
        {
        

            
         
              if
         
        (fo
        n
        )
       
         
            
         
         awa
        i
        t
         mi
        n
        i
        o.RemoveBucketAsync(rbArg
        )
        .Configur
        Aw
        ait(false);
          }

        internal static async Task RemoveBucket_Test2(MinioClient minio)
        
             
         
        var start
        e 
        =
        w
        uck        et        ame = GetR
        a
        ndomNa
        me        20        ;
               var         o        bje
        c
        t
        ame = GetRan        do        mName(2        0)        ;
         
         
             var for        ce        Fl
        gHe        ad        er = new 
        iction
        ar
           {
     
         
            
         
        "x-mini
        o
        -fo        rc        -
        elet        e"        , "tr
        u
        " 
        }
        
      
         
        

         
              va
        r
         
        b
        Args
         
         ne        w         u
        c
        ket
        E
        x
        is             
         
        cket(bucketName);
        v
        em
        B
        uck        tA        g
        Buck        et        (b        cke        tN        am        e
        )
        
            .Wit
        h
        Header        (f        o
        rceFlagHe        der        );

        

             
         
          // Create         pp        e a buck
        t
    
           va
         cou
        t = 50;
        va
        r
         tasks = ne
         Task
        [c        ];
        aw
        it Setup_
        e
        t(minio,
         
        buc
        ketName).Conf
        gureAwait(
        a
        se);
        
        f
        or
         
        (var i = 0; i
        < count; i
        +
        
        {
  
         
          
         
              tasks[i
         = PutObject_Ta
        k
        min
        o, bucketN
        a
        me, ob
        j
        ctName
         + i, null,         l
        
                rsg.G
        e
        erateS
        reamFromSee
        d5));
       
        }

   
         
          a
        ait Task.WhenAll
        (
        ta        r
        eAwait(fal
        s
        e);
      
         
         await Task.D
        lay(10
        0
        .Co
        figureAwait(fals
        e
        );        r
         args = ne
        w
         Dictionar
        y<        n
        g>
        
        {
        
            { 
        "
        bucketName", bucketName },
            { "x-minio-forc
        -dele
        e
        , 
        "true" }
    
           };
        

           
           v
        a
        r fou
        n
        d = false;

   
            try
  
         
             
        {
                  
         
         
        found = await 
        m
        inio.
        B
        ucketExistsAs
        n
        c(b
        A
        g
        )
        .
        o
        f
        gureA
        w
        i
        t(
        false);
            rt.Is
        T
        r
        u
        (
        ound);
       
         
            a
        w
        it minio.R
        e
        oveBucketA
        y
        c
        (
        bArg
        s
        .Con
        f
        g
        u
        eAwa
        it           
         
         found = await minio.B
        u
        c
        k
        e
        tExistsAsyn(beArgs).Confi
        ureA
        w
        ait(fal
        s
        e);
 
         
         
                 Asser
        t
        .IsFa
        l
        se(found);
    
            
         
          new
         
        Mint
        L
        o
        gger(nameof(Re
        m
        oveBu
        c
        kt_Test2), re
        oveB
        c
        etS
        gnature, "
        T
        ests w
        h
        ther R
        emoveBucket          
                   T
        e
        tStatus.PA
        S
        ,          
         startTime, args: args
        )
        Log();
                }
 
              catch (
        xcept
        o
         ex)

               {
            new        nameo
        (
        emove
        ucket
        _
        Test2), removeBuc
        k
        etSign
        a
        t
        ure, "Tests wh
        e
        ther 
        R
        em        sses",
        

              
         
             
         
                  FAIL,
        DateT
        i
        me.Now - startTim
        e
        , ex.M
        e
        s
        sage, ex.ToStr
        i
        ng(),
         
        ar        g();

         
             
           th
        r
        ow;
        }
   
         
            fi
        n
        a
        lly
        {

         
             
         
                  )
    
         
               
         
           aw
        a
        it        eBu
        ketAsync(r
        b
        Args).
        C
        onfigureAwait(fals
        e
        )
        
        }
    }

        internal static async Task ListBuckets_Test(MinioClient minio)
        
           
        ar st        ar        tT        ime 
        =
         DateT
        i
        me.        No        w;
                        var ar
        g
        s
        = n        w Dic        io        ary<        tring, 
        s
        ri        ng        >();
                               I
        is        t<Bucket> b
        cketLi
        st
        >(        );               
         
        var        bu        c
        etName =
         
        "bu
        tnaame";

         
            
         
        var n        oO        Buc
        k
        e
        ts         
             
        {
                                fo
        rach (var in        abl
        .Range(1, 
        n
        oOfBuc        et        ))
                                       a
        w
        a
        t Setup_Te        st        (minio,         b        uc        ket
        N
        me + indx)        .C        onfiu        re        Await(false
        ;
    
          
        ption ex)

         
            
                  {
                        
         
           
        f
        (ex.Messa        ge        .
        ta
        r
        ts        Wi        th        ("Bu
        c
        et
         
        alread        y         ow
        n
        e
        d
         you
        "
        )
  
         
         
           
         
         
         
                    /        / 
        You have yo
        lre        ad        y         created, cont
        }

         
             
         
        {
                     
             
         
            throw        
       
         
                            }
  
         
                             }

        t
        r
        y
                      
                  a          = await
        minio.
        istBu
        kets
        sync().Configure
        A
        wait(false)
        
    
                    bucketList 
         list.Buc
        e
        s;
     
         
           
           bucketList
        = bu
        k
        tLi
        t.Where(x 
        =
        > x.Na
        m
        .Start
        s
        W
        i
        th(bucketName))
        .
        ToList
        (
        ;
        
         
         As
        ert.
        A
        reEqua
        l
        (
        n
        oOfBuckets, b
        cketList.C
        u
        t);
          
          bucketList.
        oList().Sor
        (
        B
        ucket x, Bucket y) =>
                 
         
           
           i
         (
        .Name == y
        .
        Name)
         
        r
        e
        urn 0;
    
         
                  ame =
         null) ret
        u
        rn -1
        ;
                  
         
           i
        f
         
        (y.Name == nul
        l
        ) ret
        u
        rn 1;
                ret
        r
        n x.Name.
        om
        pareTo(y.Nam          
         
        })
        ;
        
      
         
             var i
        n
        dx = 0;
            foreach (
        v
        ar        uc             {
                indx++;
                Ass        (b        indx,        );             
                  tLogger(namof(ListBuckets_Test), l        gna
        ure,
        "
        ests 
        hethe
        r
         ListBucket pass
        e
        s
        "
        ,
            
         
           Te
        s
        tS        DateTime.N
        w
        - st
        a
        rtTime,
         a        og();
    
         
         }
       
         
        catch
         
        (
        xc
        p
        t
        ion 
        e
        x)
       
         
        {
        
         
         
         
         new M
        i
        n
        tL        (ListB
        u
        ckets_Te
        s
        t), listBuc
        k
        tsSignatur
        e
        , "Te
        s
        ts        tBucket pa
        s
        ses",

         
         
         
            
         
         
              
        T
        e
        tStatu
        .
        F
        IL,        w         Me
        s
        a
        g
        e, e
        .T
        S
        t
        ring
        (
        , args
         
        ar          
         
         
         
         thr
        w;
            
         
          }
  
         
         
                   {
         
         
         
            
          
         for
        e
        ch (va
         
        bu        st)
  
         
         
            
         
          {
     
         
         
         
            
         
                  =
         
        ne        etA
        gs()
         
         
                     .Wit
        B
        uck
        t(buck
        t.
        ame);
    
                  ai        cket
        As
        yn        ureAwa
        i
        t(false)
        ;
        
         
         
        }
  
         
            }

         
           }

        internal static async Task Setup_Test(MinioClient minio, string bucketName)
        {
         
            
         
        var be
        A
        rgs 
        =
         n        s
           
          .WithBuc
        k
        et(buc
        k
        etName);
       
         
        i
         (await minio.Buc        etE
        xi        ts        sync
        Args).        Co        figureAwai
        (fals        e)        )
                   
                              var mb
        A
        rgs 
        =
        new Make
        B
        uc        et        r
        s()        
               
                    .W
        i
        hBuc
        k
        e
        t(b
        u
        c
        k
         
        minio
        ke        uck        tA        sy        nc(b        Ar        g
        s
        A
           
        va        r         fo        und = a
        w
        ait mi
        n
        io.Buc        ke        tExistsAs        yn        c
        (
        b
        Args).Co        nf        gureAwait(f        al        s
        );
                     
          Asser
        .IsTrue(fo
        nd)        ;
           
         }

        internal static async Task Setup_WithLock_Test(MinioClient minio, string bucketName)
        {
                                va         
        bArg
        new 
        M
        a
        keB
        u
        c
        k
                    W
        i
        k
                .WithObje
        ar b        eA        gs
        =
         n
        ucketE
        is
        sArgs()
  
         
        e
        awa
        t mini
        .
        eBu
        ketAsync(mbArg        ).
        C
        o
         
                    ar         fou        d
         
        = awa        t
         m        nio
        .
        B
        eArgs
        .Conf
        i
        gureAwait(fal        se)        ;
 
         
              
        A
        s
        sert.IsTrue(f        ou        n
        d
        );
  
                  }

        internal static async Task TearDown(MinioClient minio, string bucketName)
        {
      
         var 
        b
        Args =
        new Bucket
        Ex        A
           
         .W        it        hBuk        et        buc
        etName);
 
        s
        ts = awa        it         m
        i
        nio.        Bu        cketEx
        i
        s
        rg        .
        Confi
        ureAw
        a
        it(false        );
                f (!
        b
        k
        tExists)
              
             
        r
        e
        r t
        skL        is        t =n        ew        Lis
        <Task        >(        );
              
         
        v
         
        =         fa        se;
                              
         
         /        /         Ge        t
        ng/Ree        ntion Info.
 
         
                      r         
        rat
        onArg
         
        
                      
                             ne
        w
         GetObjectLockConf        ig        ur        tion
        A
        r
        gs()
         
         
                     .
        W
        bucket        a
        me);
 
         
                     
        O
        b
        cC        guration
        lockCo
        fig =
        null
        
        Versioning
        C
        onfiguratio
         vers
        i
        ningCo
        fig = null
        ;
            try
     
        {
                                  v        ersi        on        in        gi        w
        ai        ti        .V        y
        nc(new Gs        ni
        gArgs()                                             .        ithBu
        c
        k
        )i        eAwa        t
        false        ;                           if
         
        (versi
        o
        ni        ng        C
        ngC
        nfig.
        t
        s.Cont        ins("E        na        be                       
                                        
         
             
         
         
        io        ni        ngCo
        n
        fig.St
        a
        tus.C        on        t
        a
        up        d")))
  
              
          {
 
            
                
         
        getVersions
        = tru
        e
        
     
              }

 
                       lockConf
        g = aw
        i
         mi
        io.GetObjectLock
        C
        on        y
        nc(lockCon
        f
        igurationA
        r
        gs).Configure
        wait(fals
        )
        
    
           }

         
               catch (Mis
        s
        ingObj
        e
        c
        tLockConfigura
        t
        ionEx
        c
        eption)
    
         
         
         {
      
                  except
        ion is expect
        d for th
        s
         bu
        kets
         
        crea
        t
        e
        d
         without a lo
        k.
        
        

             
          catch (NotImplementedException)
        {
           
        // No throw. Move to 
        he        ith
        ut versions.
        }

      
         
         v        i
        st<Task>()
        ;
        
        v
        a
        r listObjectsArgs = new ListObjec
        sArgs()
  
         
            
           .WithBucket(bucketName)
      
             .WithRecurs
        v
        (tru
        e)
            .WithVers        ions);
        v
        r
        objec
        Names
        V
        ersions =
        
         
           
        ew List<Tuple<str
        i
        ng         
             var o
        b
        jectNames 
        =
         
        n
        ew List<string
        >
        ();
 
         
                  rv
        b
        le = minio.ListO
        je
        tsAs
        nc
        l
        istObjectsArgs);
        

        
     
         
          var ex
        c
        eptionLis
        t
        = n        n = observable.S
        u
        bscrib
        e
        (
      
         
             item =
        >
        

                             (getVersio
        s
        
   
                            ersions.Ad
        (
        ew Tu
        le<st
        r
        ing, string>(item.Key, item.Ver
        s
        ionId));
            
         
         
          else
       
         
             
         
              objectNames.Add(ite
        .
        Key);
            },
            ex => 
        exceptionLis                   () => { });

        await Task.Delay(4500).ConfigureAwait(false);
        if (lockC
        n
        fig?.ObjectLockEnabled.
        Equals(Objec        ration.LockEnabled) == true)
        {
            foreach (va item in obj
        ctNam
        s
        ers
        ons)
        

            
         
         
         
             {
      
                 var ob
        e
        tRe
        entionArgs = ne
        w
         G        n
        tionArgs()
        

                  
                  h
        Bucket(bucket
        N
        ame)
        
          
              .WithO
        b
        ject(item.I
        t
        em1)
        
                   .WithVer
        io        m2)
        
   
         
             
         
              
        v
        r rete
        n
        t
        i
        o
        nConfig = awa
        t minio.Get
        b
        ect
        eten
        t
        ionAsy
        n
        c
        (
        objectRetenti
        nArgs).Con
        i
        ureAw
        a
        it(false);
     
         
                  var b
        y
        pssGovMode = 
        etentionConfi
        .
        ode
        == R
        e
        tentionMo
        d
        e
        .
        GOVERNANCE;
 
                    
         
        ar removeO
        b
        jectArgs 
        =         ject
        rgs                  ck
        t
        (bucketName
        )
        .WithObject(item.It
        e
        m1)
        

           
             
         
              
         
          .Wit
        h
        V
        ersi
        o
        nId
        (
        tem.
        I
        tem2);
  
         
         
                  passG                rem
        o
        veO
        b
        ject
        A
        rgs
         
        =         A
        rg        Go
        er
        anceMode(bypa
        s
        sGo
        v
        Mo
        d
        e)         
         
          
        v
        r
         
        t= minio.Remove
        bjec
        t
        Async
        (
        remo
        v
        e
        ObjectArgs);
 
         
             
         
                task
        .
        Add(t);
  
         
         
                }
       
         
        }
    
         
           else
        {
     
         
              if (o
        b
        ec
        Name
        sVersions.Co               
         
        {
 
            
          
              var removeObj
        ec        Re        )
 
                          .
        i
        hBu
        ket(bucketName)
      
         
                  c
        tsVersions
        (
        objectName
        sV         
             Task 
        t
         = m
        i
        nio.R
        em        v
        eObjectArgs);
        

            
         
             
         
                  
  
                 }

   
         
             
        if (o
        b
        jectNames.Count > 0)
  
         
                 {
        
         
         
              var remo
        v
        eObje
        c
        tA        Obj
        ctsArgs()
   
         
                      .
        W
        ithB
        ck
        t(bucketName)
        

                  
                  ect
        (objectNames);


         
           
                 Task t 
        =
         m        y
        nc(removeO
        b
        jectArgs);
        
         .
        Add(t);
  
         
            
         
            }
        
         a
        it Task.WhenA
        l
        l(ta
        s
        ks).C
        o
        nf        );
         
               var rb
        Ar        tArgs()
        
         
         .WithBucket(buc
        k
        etName);
        await m
        i
        nio.RemoveBuc
        k
        et        nfi
        u
        e
        wait(
        f
        alse);
    }

        internal static string XmlStrToJsonStr(string xml)
        
        ne
         
        XmlDocument()
        ;
        xml)        ;
        
                        va        r         xml
        i
        lizer = new Xm        lSe
        r
        ializer(typeo        (st        ing));
 
         
                      u        sing         v        ar         st        r
        i
        n
        Re        d
        x
        l
        ;

  
         
             va         obj         =         (strin        g)        xmlSe        ri        al        izer.Deser
        i
        a
                               ret
        u
        r
        n
         
        e
        );
    }

        internal static async Task PutGetStatEncryptedObject_Test1(MinioClient minio)
        
        Tim
         = DateTim        e.Now;

         
           
         var bucketName =
         
        G
                   v        r ob        ect
        N
        ame = GetR
        a
         
            var co        ntentType =         "        application        /octet-st
        r
        e
        empFl        eN        m
         =         "        tem
        p
        Fi        leName";
        
        v
        ar arg        s = new Dic
        t
        i
             
         
         {

         
         
         
         
        k
        me
        }
        ,
                           
         
        { "ob
        e
        t
        N
        e
        ont
        ntTyp        e"        , cont        en        tType
        }
        
  
                 {        "data",
         
        "
        si        ze        ", "1        B"         

         
               };

         
         
                //         u
        tobject wit
        h
         
            
                   a        ait S
        e
        tup_Test(minio, b        uc
        k
        etName).        Co        nfigureA
        w
        at             
        us        ing
         
        v
        a
        r
        As.Create()
                    ae
        Encr
        y
        ption.        Ke        y
        S
        ize         =         2
        5
        6
        ;
                            ae
        s
        Encry
        p
        t
        eKe
        ();        
   
         
           
          var ssec = new
         
        S
        o
        n.Key);

 
         
                                  us
        i
        ng (var filestr
        am         =         rs
        g
        .GenerateS        tr        eamFrom
        S
        eed(1 
        *
         
        KB))
                                 
        {
                      
         
                            file_wri
        e_size
        = file
        tream.Length;


         
              
           
         
        ze
        ;
         
         
                      var putOb
        j
        ectArgs
         
        = n
        e
        wPutObjectArg
        ()
                        
         
           
           .Wi        th        Bucket(
        b
        ucketN
        a
        me)
  
         
         
         
            .
        ith
        bjec        t(        oc        Name
                             
         .W
        thS
        r
        a
        mData(
        f
        ilestream)
  
         
                                          ze(f        il        es        t
                   
                             .WithSe
        verSid
        Encry
        tion
        ssec)
                    .With
        C
        ontentType(
        onten
        tT        ;
           
            await
        m
        nio.PutO
        b
        jec
        tAsync(putObj
        ctArgs).Co
        f
        gureAwait(fal
        s
        e)
        ;
        

           
            var ge
        O
        jectArgs = new GetO
        b
        je
        c
        tArgs()
     
                   
         
        .WithBucket(bucketName)
  
                     
            .WithObj
        c
        (objectName)
 
                     
            
        .
        ith
        erverSideE
        n
        crypti
        o
        (ssec)
        
                   t
        CallbackStre
        a
        (async (st
        e
        am        o
        Token) =>
  
         
                  
         
                   
                     
         
        ar fileStre
        m
         =        (
        empFil
        e
        ame);
         
                   
              
         
        wait 
        tream.CopyT
        oAsync(fileStream, cance        ).ConfigureAwait(false);
                    awai
         fileStrea
        m
        .Disp
        o
        eAsync().C
        o
        n
        figureAwait(fa
        l
        se);

         
                       
           
        ar writtenInf
         
         ne
        w
         FileI
        n
        f
        o(        );
          
         
               
         
           
        fi         = writtenInf
        o
        .Length;

 
         
         
                     
           A
        s
        rt.
        reEq
        u
        al(file_write
        _
        siz
        e
        ,f        e);
 
         
           
                  
         
           
         
        File.Delete(tempFileNa
        m
        e
        ;
          
         
                               
         var statObject
        r
        s = new St
        a
        tObjec
        tr            
            .WithBucke
        (
        u
        ck           
                .With
        b
        ect
        objectName)
 
         
                  h
        ServerSide
        E
        ncryption(
        ss         
        await mini
        o
        .StatObjec
        tA        .
        ConfigureAwait
        (
        false);
  
                  n
        io.GetObjectAs
        y
        nc(getObje
        c
        tArgs)
        .C        ;
        
            }

        
         
           n
        ew        t
        atEncryptedObje
        c
        t_Test1", p
        u
        tO             
             
         
            "Tests whe
        t
        her Put/Get/S
        t
        a
        t Object with 
        e
        ncryp
        t
        in        atu
        .PASS, DateTi
        e
        Now
        - startTime,

         
                  r
        gs).Log();
        

                }

                  e
        mentedExce
        p
        tion ex)
 
                  e
        w MintLogger("PutGetStat
        E
        ncry
        pt        O
        bjectSignature,
  
         
             
         
              
        "
        ests whether Put/
        G
        t/S        yp        .NA
         DateTime.
        o
         - s
        t
        artTim
        e
        , "",
      
         
                  Strin
        (), ar
        g
        s).Log();
 
         
              }
  
         
            catch (Except
        i
        o
        n ex)
        
        {
        
    
         
                  GetSt
        tEncrypted
        O
        bject_Test1"
        ,
         
        p
        utObjectSignat
        u
        re,
 
         
                  er 
        ut/Get/Stat
        O
        jec
         with en
        c
        ryption pass
        e
        s"        ime.Now - star
        T
        me,
       
         
              
         "        ng(), 
        a
        rgs).Log
        (
        );
            
        t
        row;
        }
        

                      
         
              
         
        await TearDo
        w
        n(        o
        n
        fi        ;
 
              }
    }

        internal static async Task PutGetStatEncryptedObject_Test2(MinioClient minio)
        
         
         
         
        teT
        me.Now;
                      
        a
         bu
        ket        Na        me = Ge        tRand
        o
        m
        o
        bjectName          G        tR        andomObj
        e
        r
         cont        nt        ype          "applicat
        i
         
          var         tempFileNa        me         = "tempF
        il        eNam
        e
        "
         = ne
         Dict        io        n
        ary<string        , stri
        n
        g>
        {
 
         
                          { "buck        et        Name"
        ,
         
             
          {         "o
        b
        jectName", o        bj        ec
        t
        Name },
                       
                   
          { "contentTy
        p
        e", c
        o
        ne         
        ",         "
        MB" },
   
         
                {         "size", "6MB" }
                        }
        ;
                        try
        {
        

        multipart P        ut         wi
        h S
        S
        E-C
         
        en        cr        yp        t
        on
   
                               await         Se        tup
        Test(m
        inio, bucketN        m
        e).C
        o
        figureA        ai        (fal        e
        ;
       
         
        cryp
        t
        on         = 
        A
        e
        s.C
        r
        e
        a();
           aesEncryp
        i
        on.KeySiz        e = 256;
                        
          
         
        io        y()        ;
                  
         
         var ssec =         ne        w SSE        C(aesEncryption.
        Key);

                    usi
        n
        g.        ener
        teStrea
        Fro
        mS        ee        (
        6
         * M
        ))
   
            
           {
     
              
            var file_writ        size = f
        il        st        a
        .L        en        gth;

                    
          
         
        ea
        d
        _size =
         
        ;

         
                         
         
         
          v        ar         
        p
        u
        tOb
        j
        e
        c
         
        gs()

         
                                            
          
         
        k
           
                  
         
            .WithO        ject(object        Na        me)
       
                   
                       .WithStrea        mD        at
        a
                      .W        th
        O
        bje
        c
        tSize        fi        estr        m.Le
        gth        )
               
                                
            .WithContentType(contentType)
                          
         
        de
        E
        cr
        y
        ption(s
        s
        c)
        ;
        
       
         
         
                              a
        w
        a
        it m        in        i
        o
        c(        utO
        bjectArgs).nfigureAwait(false);

    
        etObj
        ctArgs         =         n
        e
        w Get
        O
        jectArgs()
        

         
                                        
         
          .Wi
        t
        h
        ke        e
                
              
        .With
        bjec
        (objectName)
                  
         
         .WithServe
        SideE
        nc        ion(ssec)
   
                 
         
            .Wit
        h
        Cal
        lbackStream(a
        ync (strea
        ,
        cancellationT
        o
        ke
        n
        ) =>
        
                  
        {
                           
         
          
         
         var fileStre
        m = File.Cr
        a
        e(tempFileName);
         
                     
         await strea
        .
        opyToAsync(fil
        eStream, canc
        llat
        o
        Tok
        n).Configu
        r
        eAwait
        (
        alse);
        
                    
        wait fileStr
        e
        m.DisposeA
        y
        nc        A
        ait(false);

         
                  
         
                  w
        ittenInfo = n
        e
         FileInfo(t
        m
        pF         
              
         
             
         
                  _
        ize = 
        w
        itten
        nfo.Length;
        

                              Equal(file_write_size, file_read_size);
                 
          File.Del
        e
        te(te
        m
        FileName);
        

         
                      
         
            }
        )
        ;
            v
        r s
        atObjectArgs 
         
        ew 
        S
        tatObj
        e
        c
        tA                     
         
        .WithBu
        k
        t(b
        uc                     
         
           .WithObj
        e
        c
        t(           
            
         
           
            
         
        .WithServerSi
        d
        eEn
        c
        rp             
         
           
             await
        m
        nio
        .
        StatObjectAsync(statOb
        j
        e
        t
        rg
        s
        ).        it           
             await mini
        .
        etObjectAs
        y
        nc(get
        Oj        reAw
        it(false);
   
         
         
                    n
        w MintLogger(
        P
        tGe
        StatEncrypted
        O
        bj        t
        Signature,
        

                  
                  u
        t/Get/Stat
         
        multipart 
        up         
        passes", TestS
        t
        atus.PASS,
        
         i
        me.Now - start
        T
        ime, args:
         
        args).
        Lo         
          catch (NotImp
        l
        ementedExce
        pt         
                 new MintLogger(
        "
        PutG
        e
        tS        ct_Te
        t2", 
        p
        utObjectSignat
        u
        re,
         
         
         
             "Tests wh
        e
        ther 
        P
        u/        rt 
        pload with en
        r
        pti
        n passes", Te
        s
        tS         
             DateT
        i
        me.Now - s
        ta        g
        e, ex.ToSt
        r
        ing(), arg
        s)         
             catch (Exception ex
        )
        
   
                   
        MintLogger("PutGet
        S
        tatEn
        r
        yptedO
        b
        ect_Test2", putOb
        j
        ctS                  et/
        tat multip
        r
         upl
        o
        ad wit
        h
         encryption 
        p
        as             
              
         
          DateTime.
        N
        ow - start
        T
        me, "", ex.Messag
        e
        ,
         ex.ToString()
        ,
         args
        )
        .L        
    
           }
     
         
          finally
  
         
         
         
           {
         
         
          Fil
        e
        .D           
             await 
        e
        rDo
        n(minio,
         
        bucketName).
        C
        on            }
    }

        internal static async Task PutGetStatEncryptedObject_Test3(MinioClient minio)
         ar sta
        r
        tTime = 
        D
        ateTime.Now;
  
         
            var bucket
        N
        a
            
         
         var         o        bj
        e
        ctN        am         = GetRa
        n
        d
        va        r         co        li        a
        io        /octet-strea
                             var tem        pF        il        eName
         
        =
         
         var ar        s          
        n
        ew         Dictiona
        r
         
          {
                                 
         
          { "bucke
        t
         
             { "objec        tN        ame"        ,         objectN
        a
        me }
        ,
        

        ntType        ,         onte        nt        Type },
       
         
            { "        da        ta        ,         6MB        "         }
        ,
        
            {
         
        "size
        "
        ,
        ;
   
            t        y
               {
                      
         
                     // Test m
        u
        l
        tipart Put/G        t/
        S
        tat w
        i
        t
        in         a        a
        t Setup_Te
        st        mi        io,         ucketName).Con        fi        gureAwait(f
        alse);
                    usi
        n
        = Aes.Create();
           
         
           
         
            
         sses3 = e        w SSES
        ();

                           using 
        (var file        st        ream
         
        = rs
        gG        mSeed(6         *
         
        MB)
        

                                   
        {
            
         
            
         
                   va
        r
         
        file_write_
        estre
        m
        .Length        ;
                                l
        ng
         file_read_
           
                va
        r         pu        tObje        ct        Args = new PutObjectArgs        )

                                            .        Wi        th
        B
                                                 .W        it
        h
        Obj
        e
        ct(o
        jectName)
             
            
                             .Wit        S
        reamDa
        ta(fil        es        tream)

         
          
         
        ithObj        ec        tS
        i
        ze(
        i
        es        tream.Le
        n
        th
        )
          
         
                        
         
          
         
            .        Wi        thS
        e
        r
        v
        rSid        e
        E
        n
        cry
        p
        t
        ion(sses3)

             
         
           .WithC
        nte        n
        nt                          awai
        t
         minio.P        ut        Object        sy        nc(putO        bj        ctArgs).C
        onfigureA        wa        t(false);


                  r getObjectArgs = new Ge        Obj
        ctArgs(        
                                 
                               .WithBuckt        (b        ucket
        Name)
             
        bjectNam
        e)        
           
         
                                  
         
         .
        W
        th
        C
        allback        St        re        m(
        a
        sync (st
        r
        ea        m,        cance        ll        a
        tio
        n
        T
        ok             
         
        

               
        e
        reat        e(        t
        empFil
        e
        Name);
     
         
         
           aw
         s        ream        C
        o
        pyToA
        s
        (fileStrea
        m
        ,         ca        ncellationTok
        e
        n).Co
        n
        figureAwait(        e;                
              
             
        wait
        fileStream.DisposeAsync().Confi
        g
        ureAwait(fa
        se);

                               
            var w
        i
        tenInfo 
        =
         ne
        w FileInfo(te
        pFileName)
        

                     
         
          
         
              file_re
        d_size = w
        i
        tenInfo.Length;

  
         
          
         
                     
            Assert.
        r
        Equal(file_write_size, fil
        e_read_size);
                    
         
                 File.
        Delete(tempFi
        eNam
        )
        
  
                  
         
              
        }
        ;
    
                            c
        Args = new S
        t
        tObjectArg
        (
        )
         
              .WithB
        u
        ket(bucket
        a
        me         
                .With
        O
        ject(object
        a
        me         
             a
        w
        it mi
        i
        o.        y
        c(stat
        O
        jectA
        gs).Configu
        reAwait(false);
                t minio.GetObjectAsync(getObjectArgs).ConfigureAwait(f             
         }

      
         
             
        n
        w MintLogg
        e
        r
        ("PutGetStatEn
        c
        rypte
        d
        Ob         putO
        jec
        Signature,
  
         
           
         
              
         
        "
        Te        Put
        Get/S
        a
         mu
        tipar
        t
         
        ul        rypti
        n
         pa
        ses", Test
        t
        tus
        .
        PASS,
                
        D
        a
        e
        im
        e
        .N        me        g()
        
        }
    
         
         catch (Ex
        c
        eption
         e            
            new MintLo
        g
        r
        ("        ted
        bject_Test3",
        p
        tOb
        ectSignature,
        

                   
        whether Pu
        t
        /Get/Stat 
        mu        e
        ncryption 
        p
        asses", Te
        st         
                DateTi
        m
        e.Now - st
        ar        e
        , ex.ToString(
        )
        , args).Lo
        g
        ();
  
                   
          }
        finally
    
         
           {

                  D
        own(minio, buck
        e
        tName).Conf
        i
        gu             
          }
 
         
          }

        internal static async Task PutObject_Task(MinioClient minio, string bucketName, string objectName,
            string fileName = null, string contentType = "application/octet-stream", long size = 0,
            Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        
        art        me = D
        a
        te        Ti        me.Now        ;
         
               var         f        i
        estream = mstrea        m;        

         
         
                              if (filest
        r
        eam =
        =
         
        ar bs        = await Fi
        l
        e.ReadAllByt
        e
        s
        A
        sync(f        il        eName).C
        o
        nfi        ur
        e
        A
        les
        re        m          new Meo        ry        tre        a
        (bs);
  
         
                    }

     
         
                          var 
        i
        e_        ri        e_size 
        =
         files
        t
        mpFile
        Na        e          "tem
        p
        file-" + GetRan
        d
        mNam        e();
      
         
         
            
                   size 
        =
         filest        eam.L        n
        je        t
        ctA
        gs()
                 
         
           
        Wi        th        ucket(bucke        Na        m
        t
        hObjec        t(        bje
        c
        tName)
                    
         
        a
        ta(fi        le        trea
        m
        )
                         
         
         
        e(        si        e)
                              
         
                .WithC        n
        t
        entTy        pe        conten        Ty
        p
        e
        )
                     
         
          .Wi
        t
        hH        ;
   
             
         
          await minio.
        P
        utObjectAsync
        (
        p
        utObjectArgs).
        C
        onfig
        u
        r
        

        De        e
        e(tempFil        eN        am        e
        );
        }
                    }

        internal static async Task<ObjectStat> PutObject_Tester(MinioClient minio,
            string bucketName, string objectName, string fileName = null,
            string contentType = "application/octet-stream", long size = 0,
            Dictionary<string, string> metaData = null, MemoryStream mstream = null)
        {
                      O        b
        jectStat statObject =        null;
     
                   ar        star        Time = DateT
        i.        r filest        ream = 
        str        ea        m
        ;
 
         
                     if (files
        ream =
         nul
        )
                {                        
            va        r bs = a
        w
        ait 
        F
        ync(fil        eN        a
        m
        e).
        o
        fig        ureAwai
        t
        fa
        l
        e)
        ;
        
                
          
         
        filestre
        am         =         
        ew M
        e
        m
        ory        St        r
        e
        a
         }

 
         
        g (filestream)
  
                  le_wr
        te_siz        e =
         
        files
        t
        eam.L        en        gth;

         
         
                                  var te
        m
        pFile
        N
        a
        f
         e        domName(
        ;
    
             
         if 
        size == 0) siz
        e
         = filestre
        m.Len
        g
        h;
   
                va
        r
        putObj
        ctArgs = n
        ew PutObjectArgs
        )
      
         
            
         
         .With
        ucket(bucke
        N
        me)
                .WithO
        b
        ect(
        bjec
        N
        m
        e)
                .
        W
        ithStr
        e
        mData(
        f
        lestream
        

            
         
                 .Wi
        hObject
        i
        e(si
        z
         .WithC
        n
        en        tT        ype(co
        n
        ten
        t
           
                      .Wit
        H
        aders(m
        e
          
         
             aw        ai        t m
        ni        o
        ec
        As
        n
        (putO
        jec        tA        r
        g
        s).Configur        eA        wa        it        (fal
        s
        e);

   
         
         
                               var         st        ab        rgs          new
        S
        atO
        jectArgs()
         
          
         
         
        W
           
        Wi        hObject(objet        Na        e);

                   atObjec        t = 
        w
        it mi        ni        o.St
        a
        tObjectAsyn        c(        tatOb
        j
        e
        c
                                Assert.Is
        N
        otNt        atObe                    As
        s
        e
        ObjectName
        .
        Equals(obe        );        
      
         
                     As        sert.
        A
        Si        ze
        ,
         size);

                    
         
        !
        = nu
        l
         
                                    Asser
        t
        .IsNotNull        (s        ta        t
        ;
        
                            
         
           Asser
        t
        .
        t.Con
        entTy
        pe        .E        quals(contentT        yp        e)        );
         
         
                  }
        
                        
        Args          
        n
        ew RemoveObje        c
        tArgs()
                       .WithBu
        ket(bu
        ketNa
        e)
 
         
                  
         
          .WithObject(ob
        j
        ectName);
 
             
             await minio
        RemoveObje
        c
        Async(
        mArgs).Con
        f
        gureAw
        it(false
        ;
            
            }

        r
        turn statOb
        e
        t;
    }

        internal static async Task StatObject_Test1(MinioClient minio)
        {
   
         
          va
        r
        startTime = 
        ateTime
        N
        w;
 
                   var bucketNa        me = Get
        andomName(
        5
        ;
  
         
        bje
        tName = G
        t
        andomO        bje
        c
        tNa
        m(10);
      
         var conte
        t
        e = "gz        i
          
        v
        ar arg         = n
        w         ctio        ary<string,
           
        {

         
                                        {"bucketName"
        ,
         bucket        Na        m
        e
         }        
                   {         "        bje        ct        a
        me        "
                           
         
        nt
        nt        Ty        pe        ", cont        en        ty         
        d
        t
        a", "1KB" 
        }
        ize
        , "1KB"         
      
         
        
                       try
        

              
         
        wai
         Se        tu        p_Test        (m        ii        bk        ame).Configur
        eA        a
          
           a
        ai
         
        P
        tObj        ec        _
        ester(min        o
        ,
         buc        ke        N
        a
        , n        ul        , null,        0, nul
        ,
                    
                   rsg        G
        (
        1 * K        )).Co
        n
        figure        wa        t(
        fa         
         new M        in        Log
        ge        (name        f(        ta        t
        Ob        tO        bj        ectSign        at        re, "
        Te        st         whether
         
                                    T        s
        t
        Sta        tA        m
        e.Now          s        artTime
        ,
         ar        gs        : ar        g
         
                    ca        ch (E
        x
        ce        p
             
                   nen        Log        er(        am        o
        f(Sta        Object_        Te        s
        t
        1
        ), statObjectS
        i
        gnat        ur        e
        ,
         
        atO
        jectSignature 
        a
        ses"        
                             
         
        T
        t
        e, ex.ToSt
        r
        ing(), ar
        g
        s         
         
         
        ro        ;

             
         
         }
                        finall
        y
        
                        {
                  arDown
        (
        min        io        , buck
        e
        tName).Con
        f
        ig        se)        ;
                    }
    }

        internal static async Task FPutObject_Test1(MinioClient minio)
        {
        

        me         
         Da        t
        cketNa        me         
        = GetRand
        o
        mNam1        ;
        ame =         Ge        td        O
        b
        ject        ame(10        );
        

              
         
         var fileNa
        m
        e
         
        *
        
   
                   v        ar
        args = new D        ictio
        n
        ar        >
        
               {

         
                                 {
         
        a
        me },
    
         
                               { "ob
        j
        e
        Name 
        ,
                   { "f        il        Name"
        ,         ileNa
        m
        e
         }
                       };
          
         
            tr        

                  aw        it S
        tup_Tes        (m        n
        io        ce        e).Confi
        ureAwa
        t(fal
        e);

                   var p
        u
        tObjectArgs
        = new
         P        j
                                      
        W
        Bucket(b        ck        t
        N
           
               .Wi
        h
        bjec        (objectNa
        me        
          
           .        ithFi
        e
        am        (f        leName);
                                 
         
          
        a
        w
        Put
        bjectA        yn        (
        u
        Obje
        ig        ur
        Awai
        (
        al        e
        ;
        
         
           new
         
        (
        s
        1", putObject        Si        nature,
                     
                  t
         whether F        Pu
        t
        upl        ad pas
        ses", TestStatus.
        .N        w         -
         sta
        r
        tT
        ime        ,          arg        :
        args)
        L
           
                              
         
         new 
        M
        ntLogger("
        F
        P
        utObject_        Test
        tS        gn
        e,                              
         
        "Tests         w        ether FP        uO         
        ulti
        p
        rt u
        p
        o
        a
         pas        e
        , D
        a
        te        ow         
         
        ta
        r
        t
        T
        ime        ,
                   
                     ex
        .
        M
        tri
        g(), arg        s: 
        a
        rgs).L
        o
        g();
                    
        t
        h
        ow;
        }
     
         
         f        inall
        
      
        mini        o,         bucke
        t
        Name
        )
        Configur        e
        A
        wai
        (
        alse);
                         
         
        if 
        .Dele
        e
        (f        il        eName);
        }
    }

        internal static async Task FPutObject_Test2(MinioClient minio)
                 ar s        artTime = Dat
        me        Now
        ;

        ae        o
         
         
          
         
         var         ob        jec
        t
        N
        a
        e =         G        e
        t
        ando
        m
        O
        bje
        c
        t
        N
          v
        a
         CreateFile(10 *         K, dataFile1          var
        args = ne        w         Dicti
        o
        ary<s        tr        ing, 
        s
        t
        ring        >
                {

         
             
         
             { "buck        m"        cketName
        },
   
             
          { 
        objectName", obj
        e
        ctName },
 
             
                   "        , 
        Nam
               ty        

               {        
              
         
          
         
         a        Set
        p
        Test(minio,         b        ucketNam
        e
        ).
        C
        o
        fal
        e);
                      
        a
        ew P        tObj
        c
        Arg
        ()
                         
         
              
        .
        ithBuc
        kt(bucketNa
         
        bj        ec
        (
        o
         
                                        File
        e(         
                 aw        a
        t minio        .P        ut        bjectAsync(
        p
        s).n        se);

                          
        n
        ew M        in
        t
        ogger("FPu
        t
        O
        bject_        est2",         u
        t
        Obje        t
        S
        in        s w
        et        he        r FPutObject        f
        r sm        ll upl
                     Tes
        t
        Status.        P
        t
        art        im        , arg
        s
        : args).L        og
        (
         
              c        tc         (E
        x
        ception 
        e
        x
                             new        MintLogger
        ("        Pu        ObjeT        st2",        putOb        ec        Signa        u
        her
        Oe        al         
        upload passes",
      
        us.FAI         DateTi
        e.No        wr        Time, 
        x.Me        ssa
        ge, ex.ToStrina        s
        : a
        gs        .Log        ()        ;
            
                      
         }            
                   
        a
        ckeN        am        ).Config
        u
        re        Aw        ai        t(false);
             if (I        s
        M
           {
                GC.Collect        );
                      
                                       G        WaitFr        i
        er        s(        );
   
         
           
                   Fi        le        .D        ele
        t
                              }                     
         
         
        }
            
        }

        internal static async Task RemoveObject_Test1(MinioClient minio)
                 ime =
         
        ateTim        e.        Now;
                       var b        uc        ketN        me        = Ge
        t
        R
          
        b
        jectN        me         
        = GetRan
        d
        omObjectName        ;            var a
        gs = n
        w Dic
        iona
        y<string, string
        >
        
        {

             
                  { "bucketName
        , bucketN
        m
         },
          
         
                     {         "o
        j
        ctName", obje
        c
        tN
        a
        m
        ;
 
                      tr        y
                   
         
         {
                                                   usin        g         (v        af        st        r         rsg.Genera
        t
        eS
        r
        am
        F
        omSeed(1 * K
        B
        ))
                   {
  
         
           
                       a        ait
         
        Setup_
        Ts        t(        mi        nio,
         bucketName
        f
        lse);
      
         
                var        p
        u
        e
        utObjectAr
        gs()
                          
         
         
        k
        t(bucketNa
        m
            
                   
           .WithObjet(objectNa
             
         .Wi        hS        ream
        D
        ata(f
        i
        estream)
                    
                                                  .
        W
        i
        fe        );
        
                                     
        a
        As        nc(putO        bje
        c
        tArgs).C
                                
        t
        Logger("R        m
        o
        veObject
        _
        T
        bjec        S
        gnatu
        r
        e1,
          
                            t
        s
         
        whether Remove
        O
        bject
        A
        s
        g         ob
        ect         pa        ses",
         
        TestS        ta        tu        s.PASS, Dat
        eTime.        No        w - sta        rt        Time,
  
         
                           arg
        : args).Lo
        ()        

             
         }
         ch (        Ex        ceptio
        n
         )
        {        
      
         
                    ne        w
        Mint
        L
        o
        gger        (
        ", re
        o
        ve        Ob        jectSiga        tu        r
           
            "Tests         w
        hether RemoveObjec
        t
        sync for existino        ct s        es", T
        stS
        atus.A        IL, Da
        eTime.
        No
          
         
            
         
           ex.M        s
        s
        age
         
        x.ToStrin
        g
         
        a
        rgs: a        rg
        s
        .L
        o
        g();
   
         
         
         
                     t
        ro        ;

         
         
           
         
         
         
        lly
 
         
         
        Ne        .Config
        u
        r
        eAwait(        fa        lse);
 
         
             
         
        }
    }

        internal static async Task RemoveObjects_Test2(MinioClient minio)
        {
        var start        ime
         
        =
         
            
         
         var         bc        t
        5
                   objectN
        me = G
        tRand
        mObj
        ctName(6);
       
         
        var objects
        ist =
         n        ist<string>()
        
        
        a
        gs = ne
        w
         Di
        ctionary<stri
        g, string>
         
                              
        "b        ucketNam"        ,
        bucketN        am        e },
       
         
          
         
         { "objectNam
        s"         "
        "
        +         b
        ectN        me +         0
        .
        .." + 
        o
        jectNam        e+ "50]" }

         
          try
               
        
                         v
        a
         
                               var 
        tasks =         ne        w T        as        kn        ]
           
        p_        bucke
        N
        ame
        .Configure
        w
        it(
        f
        alse);
                                                  fo         (var 
        i
         
         
        ;         i         <
         c         
             
        tasks[i        ] =
         
        PutOb
        je        t_Task(minb        Na        e, 
        o
        b
         nu
        l,                  l,                             
         
         
        ro        S
        e
        ed(5));
          
        t
        .Add(objec
        t
        Name + i
        }
        

                            aw
        a
        it         T        sk.When
        A
        it        (f        a;                  
         
        await 
        T
        a
        gA        wait(
        f
        alse);
                        
         
         
         
        ",         remov
        e
        Objec        tS        ignature2        ,
                     "Tes        s whet        he        r 
        R
        or mul        t
        bjects 
        elete p        as        ses",         Te        st        a
        us.        ASS
        
     
          
              Date
        T
        ime.
        N
        w -         start
        T
        ime
         
        rgs: args
        )
                     
        catc
        h
         
        (Ex        e
        p
        t
             {
        
         
          new Min        tL        gg
        er("RemoveO
        remo        eObje        ct        Si        gnat
        u                                 "Tests whether Re
        m
         multi ob        jects 
        elete passes        ",        Test
        tat
        s.FAIL,

              
              
          Dat        Time        Now 
        -
         st
        a
        tTi        e, ex
        .M        es
        gs
        )
        .Log        ()        ;
 
         
         
            thro        w
                     
         
        
                      
         f        in        a
        l
        l
        y
                     
        a
        etN
        
   
         
        }

        internal static async Task RemoveObjects_Test3(MinioClient minio)
        {
                var startTi
        m
        e = DateTim
        .Now;
        
            var bucket
        ame = Get
        d
         va
         objectNam
         
         Gen        dm        ctN
        me(6);
           
         
         v        r args        = new        Dictio
        n
        a
        r
        y
        rin
        >
               {

                     
            
        {
         "buck
        e
        t
        N
        ae        },
         
                                         { "obje
        c
        tName        "
        ,
        "[" + 
        o
        ectName + "
        50        " }
      
         
        }
         
           {
               
            v        r         o
        nt          50;
  
         
                               
        var t        as        s = new 
        a
        k[cou
        t
           
        is        t
         awi        t Set
        p
        Wit
        Lock
        _
        Test(
        m
        .Confi
        g
        ureAw
        a
        t(fals        );        
            
               for        (v        r 
        i         =         0; i
         
        <            
         
                     
         
         
                       
        t
        s
        ks
        [
        minio
        ,
         
        b
        c
        Name,         objectNam
        e
         + i,
         
        ull, null,
         ,         n        u,                    
         
         
         
         rsg
        .
                                 
         
         
         
         
         
        +]         
        = P
        u
        tObjec        t_        Task
        m
        nu        ea        me         +         i,
        null
        ,
         null        ,0          
         
             
         
         
        treamr        om        Sd        (5));
   
                                
         
         
                         }
        
            awa
        it        sks).ConfigureAw        at        );
                           
        it Ta
        k.Dela
        y(1000).Con        ig
        u
        reAw
                     va        r l
        is        tO        j
        c
        sArgs         =         new
        Obj
        e
        tsArg        s
        Bucke        t
        c
        ketNa        me        )          
        rsi
        e(true)
           
         
                         hVersions(true);
              
                  le = minio.ListObj        et        (list
        bjectsA
        );
  
              
           var o        jVer        i
        o
        ns =
         
        g, strin
        g
        >>(;        
                                  v
        a
         su        b
        scr         ser        va        b
        le.
        S
        u
        b
             
        ions.        Ad        d(        n
        tem.Key, item
        we                
              
        sync 
        ) =>
                        {
 
         
                   
             
         v        emoveObjectsA
        gs = new 
        e
        oveObjec
        t
        sAr
        gs()
        
                  
         
          .WithBucket
        (
        bu
        c
        ketName)
    
                  
         
              .WithObjectsV
        e
        r
        s
        ions(objVersi
        ns);
        

           
                  
         
            va
        r
        rmObse
        rvable = awa        o
        eObjectsAsyn
        c
        removeObje
        t
        sA        r
        Await(false);
        

           
         
                  
         
         var d
        L
        st = new L
        s
        <Dele
        eError>();

                            usin        = r
        Obser
        a
        le
        .S           
             
         
           
            
         
         err 
        >
        d
        e
        Li        
  
                   
         
           
           e
        x
         => th
        r
        o
        w
         e             
                 async () =
        >
         awai
        t
        TearDown(m
        i
        n
        io, bucketName
        )
        .Conf
        i
        gu        e))
        

           
         
         
         
         
         
         
         });

         
         
         
                  Ta        0).Co
        n
        f
        ig
        u
        e
        wait(false);
 
         
             
         
           new Min
        t
        ogger("Rem
        v
        O
        b
        ects
        _
        est3
        "
         
        r
        move
        Ob           
         
                "Tests whether
         
        R
        e
        m
        ov        r mul
        t
        i
         o
        b
        e
        ts/versions de
        l
        ete p
        a
        ses", Test
        S
        atus.PASS,
         
         
         
            
         
            
         
        a
        t
        Time
        .N        : a
        r
        gs).Log();
        }
 
         
         
         
         
                  mentedExcep
        t
        ion
         
        ex)
      
         
        

         
                   i        moveO
        ject
        s
        _Test3"
        ,
         remo
        v
        e
        ObjectSignatur
        e
        2,
  
         
                  Tests
        whet
        h
        er Re
        m
        oveO
        b
        j
        ectsAsync for 
        m
        ulti 
        o
        bj        s d
        lete passes", T
        s
        Sta
        us.NA,
        
         
                  w
         - startTi
        m
        e, "", ex.
        Me        n
        g(), args).Lo
        g
        ();

                  c
        atch (Except
        i
        on e
        x
        )
           
              new 
        i
        tLogg
        e
        r("RemoveObjects
        _
        Test3", removeO
        b
        je        ,
 
                   
         
        "Te
        ts w
        h
        ether
         
        Remove
        O
        jectsA
        s
        y
        n
        c
         f        ect
        /versions de
        e
        e passes",
         
        TestStatu
        s.            
         D
        teTime.Now 
        -
         st
        a
        rtT
        me, "
        "
        , ex.M
        e
        sage, 
        e
        x
        .ToS
        t
        rin
        g
        ), a
        r
        gs).Log()
        ;
        

                  
 
          
           }

          
         }

        internal static async Task DownloadObjectAsync(MinioClient minio, string url, string filePath)
        {
                      
         
         using va         r
        e
        s
        Get        As        nc(url)        .Confi
        u
        eAwai
        (fals
        e
        );
               if (stri        ng        IsNullOrEmp        y(        Conve
        r
        t
        .ToString        respo
        n
        se.Co
        n
        t
        Equ
        ls        re        po
        s
        .St
        tu        Co        e
        ))
                
         
         
         
        tr        xcept
        on(
        ameof
        r
        sp        nse.Co        te        t),         
        "Unable t
        o
        
  
                  us        in        g va
        r
         fs
         
        = n
        e
        w
        e.        Cr        at
        New        );        
                     
         
        pyToA
        y
        n
        (f        s)        Confi        gu        eAwait(f
        a
        ls        e)        ;
          
         
        }

        internal static async Task UploadObjectAsync(MinioClient minio, string url, string filePath)
        {

            
         
         
         us        in        g         var         f        le        trea
        m
         = ne
        w
         
        th,
        File        Mo        e.Ope
        n
        , Fil        Ac        es        s.        ead);        
                       
        g         ar st        ream = new Strea
        m
        ;
        awai
        in        io        .W        rapperPutAsync(
        rl,
        stre        am        ).        on        fi        ur        Aw
        a
        it(false
        ;
                    }

        internal static async Task PresignedPostPolicy_Test1(MinioClient minio)
        {
        

            
         
         v        ar         s
        t
        a
        rtT
        i
        m
        e = DateTim
         var 
        u
        cketName = GetRandom        Na        me(
        ;

         
        Ran
        omObjectNa
        m
        e(10);
                       va        r meta
        da        ta        ey =         Ge        tR        andomName(10);
                   
         
        ue = Ge        Rando        mN        am
        (10        );        

                                // Gener
        te r        es        igne         po        st pol
        i
        cy url
 
                     v
        r form
        Polic         =        new         os
        t
        Po
        l
        expiresO
        n
         = D        t
        Time.Utc        No        w
        .
        dd
        M
        nu
        te        s(15);

         
          
         
           formP        o
        l
        i
        c
        .Set
        E
        x
        pir        es        (
        e
        xiresOn);
        form        Poli
        y
        .SetBuc        ke        t(
        uc        ke        tName);
   
        y.S
        ey        object        Na        m
        e
        );
        form        Po        licy.
        SetUserMeta        da        ta(metadataKey
        ,
                     v
        r         ar        gs = 
         Dictio        na        y<strin        g,         s        tr        in        >
 
             
        {
              
             { "
        uck        tN        m
        ", buc
        ke
        me },
             
         
            
         
        bj        ctName
         
        },
                                          {         ex
        p
        re        sO        n
        , 
        e
        xp        ir        sOn.
        TS        tr        in        g
        () }        
                               }
        ;
        
                     
         
         
         //
         
        Fi        lt        d
   
         
        e
         B              va
         sizeE
        pecte
         = 1
        240;
        var co
        n
        tentType = 
        appli
        c
        tion/o
        tet
        -
        tream"
        
       
         v        i
        eateF
        le(
        iz        e,         i        KB);

  
         
           
         
         
        try
                      {
           
                                
        // Creates t
        e
         bucke
        t
        
            
        a
        wait S        et        up        _
        Test(min
        i
        o, buc        ke        tN
        a
        me)        .C        oe        ait(        al
        s
        e);

                    
                     var po        l
        A
        edPos
        Pol
        cyArgs().WithBucket        bu
        ck        tName)                        
         
              .
        W
        ih        Object(obj        e
             
          .        Wi        hP
        c
        (f        or        mc        

         
                         
         
        v
        t mini        .P        re        i
        gn
        e
        d
        PostPolicyAsyn
        c
        (p        ol        Arg
        s
        )
        ue        t(false)
        
     
             
        var 
        ri = policyTuple.
        I
        tem1.Absolu
        eUri;
        

              
           
         
        var cu
        lCommand
         =        r
        ;
   
           
            foreac        h         v
        r p        a
        yT
        up        e.Item2)
         
        curl        Co        m
        and += $" 
        -
        F {p
        a
        i
        a
        r.V
        lue}\"
        ;
           
                       urlCommand += $" -F fil=        \"@{
        f
        ileName}\" {uri
        }"        


                        
         
         
         Bash(curlCo        mm        an
        d
        );

                              alidate

              
            v
        r st
        tObjectArgs = new StatObj
        e
        ctArgs()
  
             
                    .WithBucket
        bucketNam
        )
                
         
           
            .WithObje
        t(objectNa
        e
        ;

          
         
         v
        a
        r statObject 
         await min
        o
        StatObjectAsync(sta
        t
        Ob
        j
        ectArgs).Conf
        gureAwait(f
        l
        e);
         
         
          
        A
        ssert.IsNotNu
        l(statObject)
        

                   As
        s
        er
        t
        .IsTrue(statObject.ObjectName.Equals(objectName));
         
          Assert.A
        e
        qua
        (statObjec
        t
        .
        Size, sizeExp
        cted);
  
         
               A
        s
        sert.I
        s
        True(statO
        b
        je
        c
        t.MetaData["Content-
        T
        ype"] != n
        u
        ll);
    
         
               Assert.IsTrue
        (
        statObjec
        t
        .ContentTy
        p
        e.Equals(contentType
        )
        );
   
         
                As
        s
        ert.IsTrue(statObjec
        t
        .MetaData[metad
        a
        taKey].Equa
        l
        (metadataValu
        e
        );

         
          ne
         
        int
        ogger("Pre
        s
        ignedP
        o
        tPolic
        y_Test1", pr        o
        icySignature
        ,
                  
         
                  t
        er Presigned
        P
        stPolicy u
        l
         a        y
        on server",
         
        estStatus
        .
        PASS,
  
         
         
                   
        DateTime.Now - startTime, args: args).Log();

            
         
        }

         
          
           catch (Exc
        ption ex)
  
         
           {

                    n
        w MintLogge
        (
        PresignedPostPolicy_Test1"
        , presignedPo
        tPolicyS
        g
        ature,
   
         
            
         
              "Tests
         
        wether PresignedPostPoli        es policy on server",         AIL,

                  
         
            D
        a
        eTime.Now 
        -
         
        startTime, ex.
        M
        essag
        e
        ,e        , a
        gs: arg
        )
        Log
        );
            throw;
 
         
         
         
            }
    
         
           finally
        
          
            await 
        T
        earDown(mi
        ni        o
        nfigureAwa
        i
        t(false);

         
                     
        f (!IsMintE
        v
        )) Fi
        e.Del
        e
        te(fileName);
    }

        internal static async Task RemoveIncompleteUpload_Test(MinioClient minio)
        {
  
          
        v
        r startTime
         =         D        ateT
        i
        me.Now;            
         
         Ge
        Ran        omName(15        ;
            v        r
        etRando
        O
        bj        c
        Na)                      v
        a
        r con
        t
        Type = "        sv"
        
 
         
                             v
        r
         a        rg        s         ne         
        D
        ic        ion
        a
        r
        y<        t
               {
  
          
              { "bucketN        am        e", buc
        ketNa
        me }        ,

         
        ctNa
        m
        e", objectN        am        e
         }
               try

                   
          a        ait Setup_T
        s
        (mi
        io, buck        et        Name).
        C
        o
        ;
        
                   using var        c
        T
        okenSourc        (
        )
        ;
                          
        Ti        me        S
        an.FromMil
        conds
        2));                           try
  
         
                                         {
               
         
         
                      using va
        r
         f        le        t
        r
        e
        eS        tream
        F
        romSeed(        0
         
        * MB);
   
         
        fil        e_        wri        te        _
        size =
         
        filestream
        .
        Length;

 
         
              
         
               var
         
        p
        u
        new Pu
        t
        Ob        ec        Args        )
        
                                         
         
         .WithBu        ket(
        b
        u
                                
         
                              .W
        i
        thObje        ct        (ob        je        c
        t
        Name)
  
         
                                       
          .WithS        tr        ea
        m
        D
        
     
         
                                      .With        Ob        j
        ectSize(fi        le        s
        t
        ream.L
        e
        ngth)
                         
         
        Con        te        ntT
        y
        pe(con
        t
        entType);
                   
                
         
            aw        ai        t min
        i
        o
        .PutO        bj        c
        tAsync(putObj        ec        t
        A
        r
        Co
        reAw        it(fa
        l
        se);
                                           }
                    
        catch (OperationCan        eledException        )
                   v        r 
        mArgs = ne        w         emoveInc
        mp        et        UploadA
        gs()
            
              
          
          .WithBuc        ke        t(bu
        c
                
        .
        Wit
        O
        ject(obje
        c
        Name
        )
        

  
         
         
           
         
         
         
        m
        ncomp
        e
        teUp        oadAs
        nc
        (
        fg        se        ;
        
                              var listArgs = new Lis
        tI        n
        ompleteUploa        sArgs()
                        
         
        bucket
        e);
   
                    var obs
        rva
        le = mi
        io.Lis
        In
        omplet
        eUploads(listArgs);
        r         su        scrip
        t
        ion=         
        ervable.S
        u
        sc
        ri        be        (
                       
          
         
                
         
        i
        t
        m         > 
        A
        sert        Fa        l(),         
                       
        e
        a
               }

       
        ogger"        Re        moveInc
        o
        mpl        ete
        U
        load_Test"
        ,
         
        rem        oveIncomple        te        U
        p
        loadS
        i
        g
         
         
         
         
        "Tests w        et        e
        r
        Rem        v
        e
        Inco        mp        le
        t
        eUpload 
        p
        as        "         Status.P
        SS, Da
        eTime
        Now 
         startTime,
               
         
            args: a
        gs)
 
                           .Log
        );
      
         
        
       
         
        cat
        ch (Exception
        ex)
      
         
        
            
        n
        ew
         
        MintLogger("R
        moveIncomp
        e
        eUpload_Test", remo
        v
        eI
        n
        completeUploa
        Signature,

         
             
                "Test
         whe
        h
        r R
        moveIncomp
        l
        eteUpl
        o
        d pass
        es.", TestSt        a
        eTime.Now - 
        s
        artTime, e
        .
        Me         
                 ex.
        T
        String(), 
        rgs: args).
        Log();
            throw             
          finally

         
             
         
        {
        
         
         
          await TearDo
        w
        n(min
        i
        o,        .Conf
        gur
        Awa
        t
        fal
        e);
        }
    }

        #region Select Object Content

        internal static async Task SelectObjectContent_Test(MinioClient minio)
        {
                        var
         
        st
        T
        me
         
        =
          var bucket        Name
        =
        tRandomName        (1        5);
  
         
        Get
        do        mb        tNa
        e(1        0)        ;
        
        v
        a
        N
        ame";
    
                            v
        i
        ng        ,         string>

         
                       {        
   
         
        b
        ucketName         },        

        t
        Name },
               
         
            {         "f        ileN
        a
        me        ",         out
        F
              try        
           
         
                    {
               
         
         
        minio
         buc        ke        tN        a
        me).Confi        gureAw
        a
        it(false);        
                              ar csvS
        t
        rin        g         = 
        n
        ew        e
           cs
        S
        tring.AppendLine("Employee
        ,n           
             
        s
        Str
        ng.AppendL        ne("Employee4,        mp
        l
        o
         
         csvString
        .
        App        e
        ee        3,Em        lo        ee1,
        5
        00");        
                       
        "l        e1,,10        0"        );
                           cS        g.AppendLin
        e
        ("Emp
        l
        o
        );

                
         
        csv
        trin        g.Ap        endLine("Employee2
        ,
        E
           varc        s
        v
        B
        F8.
        etBy        es(c        vS
        r
        ng.        oS
        t
        ring        ));                            usi
        n
        g (var s
        t
        r         w
        MemoryStr        ea        m(c
        s
        tes))
 
         
                                         {

         
        ct        Ar        s
        = e        w         utOb
        j
        ectA
        r
        g
        s
        .B        tu         
           .W
        ithObject(o        bj        ectName        )
                 
                   
            .WithStreamDat        a(        stream        )
         
        thObjectSize(s        tr        am.Len        gt        h);
                                       aw        ait m
        i
        nio.PutObjectAsy        c(
        p
        tOb        ectArg        ).C
        gureAwait
        (
            
         
                    
         
        t
        ion          
        n
        iali        za
        i
        on
                 
         
        si        on
        e = Select
        C
        omp        essionType.        ON        ,
          
              CS        V         = n        ew         CSVInputOptio        s
       
         
                        Fi        eH        a
        er        nfo = CSVFi        le        HeaderI        nf        .
        one,
          
         
                         Record        elimi
        t
        er 
         \        n"        
      
         
                                   
        F
        "

                                  
         
         
         
        
  
         
            
         
                   };
        

         
                   outp
        utSerializa
         Select
        e
             
          {
                      
         
                               
         
        CSV = new 
        C
        S
        VOutp        tOpt        ons
                               
         
             {
                            RecordDelimiter = "\n",
                   
            Fi
        ldDel
        mite
         = ","
                }
        

                   
        };
  
                      var selAr
        s = new S
        l
        ctObject
        C
        ont
        entArgs()
   
                  
         
        WithBucket(bu
        c
        ke
        t
        Name)
       
                .W
        t
        Object(objectName)

         
          
         
                    .
        ithExpressi
        n
        ype(QueryExpr
        essionType.SQ
        )
  
         
           
               .Wi
        t
        hQuery
        E
        pressi
        on("select *        c
        ")
         
         
             .With
        n
        pu        o
        (inputSerial
        i
        ation)
   
         
                  t
        OutputSeri
        a
        ization(out
        utSerializa
        tion);

            var          mini
        .SelectObj
        e
        ctCon
        t
        ntAsync(se
        l
        A
        rgs).Configure
        A
        wait(
        f
        al           
         using va
         
        tre
        mReader = new
         
        S
        tr        sp.Payloa
        d
        );
       
         
            var output = await s
        t
        re        dToEndAsy
        n
        c().Config
        u
        reAwait(false);
         
         
                  gNoWS = R
        e
        gex.Replac
        e
        (csvString.ToString(), @"
        \
        s+                v
        a
        r outputNo
        W
        S = Regex.Replace
        (
        ou        , "");
  
         
                 /
        /
         Compute MD5 for a better
         
        re              var
         
        hashedOutp
        u
        tBytes = MD5.HashData(Enc
        o
        di        yte
        (outputN
        W
        ));
    
         
            
         
          var ou
        t
        putMd5 = 
        C
        onvert.T
        o
        B
        a
        se        hedOu
        p
        utB
        tes);

         
           
             var has
        h
        edCSVByt
        e
        s         ta        etB
        tes(csvString
        o
        S))
        
            
        v
        ar        B
        ase64Strin
        g
        (hashedCSV
        By        s
        sert.IsTru
        e
        (csvMd5.Co
        nt         
                 new M
        i
        ntLogg
        er        t
        _Test", select
        O
        bjectS
        i
        gnatur
        e
        ,
        "Test
         whet
        h
        er SelectObjec
        t
        Content passe
        s
         
        for a select q
        u
        ery",
         
        Te        S
           
        DateTime.Now - sta
        t
        ime
         args: args).Log();
        }
          (              {
       
         
          new MintLogger("Sel
        e
        ctOb
        je         se
        e
        tOb
        ectSignature,
          es        ectContent pas
        e
         for a select que
        r
        y", 
        Te                   Date
        i
        e.No
        w         age, ex.ToStri
        g
        ), a        ;
        t
        hr        }
 
              finally
     
         
        {
 
                  await TearDown(minio,         Co        se)
        

           
               File.Delet                }
    }

        #endregion

        #region Bucket Encryption

        internal static async Task BucketEncryptionsAsync_Test1(MinioClient minio)
        {
 
         
                    
        e
        Tim        e.        Now;
                       var         bu
        c
        ketName = GetRandomName        15
         
        = new        Di        ctionary<string,        s
        tring>
                       {
   
         
        m
        e",         ucketName }        
               }
        ;
        
               try
                
        {
        
         ait
        Setu
        _
        est(m
        nio,         u
        cketName).        Co        nfigur        eA        ait(fal
        s
        e);        
                      
         
         
        }
                ca        tch 
        (E        xc        epti
        o
        n
        
    
          
        awai         TearDo
        n
        min
         b        ucketName).
        C
        onfi
        g
        ureAwai        t(        f
        a
           
        ew Min
        L
        gger(
        am        eo        f(BucketEn
        c
        ryptionsAsync        _T        e
        s
        t
        1
        ), setB        uc        ketEncr
        y
        ption
        S
        i
           
              "Tests 
        h
        th        er         Se
        t
        BucketE        nc        r
        yptionAsy
        n
        c passes
        "
        ,
         
        estStatu
        s
        FA
        I
        L,        w         -         s
        artTime,         ex        .
        e
        sage,
        

                                        
        e
        .ToStr
        i
        g(
        )
        ,
        g(        );               
           
        thr
        w
        
     
        }

   
                     
                           va        r encr
        p
        ion
        A
        rgs = ne
        w
         Set        Bu        cket
        E
        ncry
        p
        tionArgs        )
        
                          
         
         
        ket
        ame        );        
     
                             await
         
        mi        ni        o.        Se        tBucketEnc
        r
        yptionAsy        c(        encr        pti
        o
        n
        wai
        (fal        se        );
                         
         
        new
         
        MintLogg
        e
        r(nameof
        (
        Buck
        e
        tEncr        pti
        o
        nsAsync_Te        t1        ,
         
        s
        on        Si
        nature
        

               
         
                           "Tes        ts         
        whether Se        tBuck
        e
        tn        c p        asse
        s
        ", Tes        tS        t
        atus.P
        A
        SS,         DateT
        i
        me.Now - 
        s
        t
        a
           
                                  a        rg
        s
        : args)
                        .L
        og();
                        }
                        catc        h         (N        eption        x)
                      
         {
                            new Mi
        tLogge        r
        meo
        (
        ucketE
        cr        yp        tio
        ns
        nc_Test1),
         
        setB
        u
        ature,
 
         
           
         
                              "Tes         whe
        t
        er S
        e
        t
        Buc
        k
        e
        tEncryptionAsync passes",
        T
        estStatus
        NA
        , DateTime.
        , e
        .Message,

         
                                               ex.ToString()
        , args         a        gs).        og();
      
                  (Exception ex)
        {
            aw        it        ear
        o
        n(mini
        , bu        ck        e
        tNam        e).Configu
        r
        eAwa
        it                     ew        M
        i
        ntL        o
        g
        r(nameof(
        B
        ck
        e
        tEncryp
        t
        on
        s
        Async_T        st        )
        ,
        setB
        u
        ketE
        n
        c
        ryp
        t
        i
        on                      
                 "T
        er SetB
        t
        s", T
        stStatus
        .
        FAIL,
         
        ateTime.No
        w
         
        - startTime, e
        x
        .Mess
        a
           e
        x
        .ToStr
        i
        ng(), args:
         
        args).Log();                     throw;
            }

        try
        {
                v
        r encr
        ption
        rgs 
         new GetBucketEncryptionArgs
        (
        )
         
             
         .        Bucket(bucket
        ame);
   
         
              va
        r
         co
        nfig = await 
        inio.GetBu
        k
        tEncryptionAs
        y
        nc
        (
        encryptionArg
        ).Co
        f
        gur
        Await(fals
        e
        );
   
         
              
         Assert.IsNo        )
        
           
         
        ssert.IsNo
        Null(config
        .Rule);
            Asse        (conf
        g.Rule.App
        l
        y);
 
         
                 A
        s
        s
        ert.IsTrue(con
        f
        ig.Ru
        l
        e.Apply.SSEAlgorithm.Cont
        i
        ns("AES25
        ")
        );
                 ogger
        nameof(B
        u
        cketE
        n
        ryptionsAs
        y
        n
        c_Test1), getB
        u
        cketE
        n
        cr        ure
        
         
         
              
         
           "Tests whether GetBucketE
        n
        c
        yptionAsync passes", TestSta
        tu        .Now - startTime,
                    args: arg
        s
        
         
         
            
         
        .Log();

         
           
         
         }
      
         
        ca
        t
        ch (Not
        Im        on
         
        ex)
    
         
         
         
        {
  
         
            
         
         
          n
        e
        w
         M        meof(
        BucketEncrytionsAsync_Test1), getB        ion
        ignature,
    
         
           
             "Tests whether Get
        B
        uc        n
        c passes",
         
        TestStatus
        .
        NA        ow - 
        tartT
        i
        me, ex.Message,
        
         
               ex.ToSt
        r
        i
        ng(), args: ar
        g
        s).Lo
        g
        ()           
            catch 
        (
        Except
        i
        on ex)
        {
           
         
        a
        ait TearDown(minio, bucketNa
        me        se);
            new MintLogger(nameof(BucketEn
        c
        yptionsAsy
        n
        c_Te
        s
        1), getB
        u
        cke
        E
        cryptionS
        ig            
        "
        ests
         w        E
        ncr
        y
        p
        tionAsync passes", TestSt
        t
        us.FAIL, DateTime.Now -
        st
        artTime, ex.           
                 e
        x
        .ToStr
        i
        ng(), args: args).Log();
   
         
         
              throw;
        }

    
                              var rmEncryptionArgs = new RemoveBu
        c
        etEncrypti
        o
        nA
        r
        s()
    
         
           
         
             .Wit
        h
        uc
        k
        et(buck
        et          
         
        await mi
        n
        i
        o
        Remo
        v
        Buck
        e
        t
        Enc
        r
        y
        ptionAsync(rmEncryptionAr
        s
        ).Configu
        eA
        wait(false);         var 
        ncryptio
        n
        Args 
        =
        new GetBuc
        k
        e
        tEncryptionArg
        s
        ()
  
         
                  Wit
        Bucket(buc
        k
        etName
        )
        ;
            var config = a
        w
        a
        t minio.GetBucketEncryptionA
        sy        s).ConfigureAwait(false);
        }
        cat
        c
         (NotImple
        m
        ente
        d
        xception
         
        ex)
         
              {
 
         
          
         
              n
        ew        of
        (
        BucketEn
        c
        r
        y
        tion
        s
        sync
        _
        T
        est
        1
        )
        ,         Encry
        ptionSignatre,
                "Te        Rem
        veBucketEncryp
        i
        nAs
        nc passes", TestStatus.
        N
        A,        s
        tartTime, 
        e
        x.Message,
        

                    e
        .ToStr
        n
        (), a
        gs: a
        r
        gs).Log();
        }
   
         
            catch (Exc
        e
        p
        tion ex)
     
         
          {
 
         
                  ex.Mes
        s
        age.Conta
        i
        ns("Th
        e
         s        ncrypt
        i
        on config
        u
        ration
         
        was 
        n
        ot              
         
           {
    
         
              
         
            
        n
        ew Mi
        n
        tL        (Bucke
        t
        Encryp
        t
        ionsAs
        y
        nc_T
        e
        st1),
         
        removeBucket
        E
        ncryptio
        n
        Signatur
        e
        ,
        
            
           "Tests 
        w
        hether
         
        RemoveBucketEncryptionAsync 
        p
        a
        ses", TestStatus.PASS, DateT
        im                           args: args).Log();
         
         
         }
       
         
            
        e
        se
     
         
           
         
        {
       
                  r(na
        m
        of(B
        uc        y
        nc_
        T
        e
        st1), removeBucketEncrypt
        o
        nSignature,
           
          
              "Tests        ove
        ucketEncry
        p
        tionAs
        y
        nc passes", TestStatus.FAIL,
         
        D
        teTime.Now - startTime,
    
                  .Message, ex.ToString(), args: args).Log();
   
         
                  
         
        th
        r
        w;
     
         
           
         
        }
       
         
        
 
         
              f
        in          
         
                
        a
        w
        a
        t Te
        a
        Down
        (
        m
        ini
        o
        ,
         bucketName).ConfigureAwa
        t
        (false);

          
             }
    }

        #endregion

        #region Legal Hold Status

        internal static async Task LegalHoldStatusAsync_Test1(MinioClient minio)
        {

         
         
         
            
        v
        r s        ta        r
        t
        T
        ime         =         
        D
        ateTime.Now
        var bc        ke        tName =         G        et
        an
        domName(15)
        bj        ctN
        me = Get
        R
        andom
        O
        jectName(        10
        )
        ;
        
        var a
        r
        gs = 
        n
        ew        str
        ng, string
        >
        
     
         
          {
                           { "buck        et        Name"        ,         u
        c
        k
        tName         }        ,
                          { "o        bjectNam
        e
                     };               
        try
                {
                                    aw
        it Set
        up_WithLoc        _T        es
        t
        (min
        i
        , bucket
        N
        ame
        .C        nfi        gureAwa
        i
        (f
        a
        lse);
 
         
        tc        h         (
        NotImp        le        me
        n
        t
        e
        Exce
        p
        ion 
        e
        x
        )
 
         
         
         
          awa
        it TearDown
        ketName).Confi        lse
        ;
            ne
         
        int
        ogger(nam        eo        (LegalHoldS        ta        tus        As        n
        c
        _
        l
        Hold        Si        nat        ur        e,
        

                           
         
         
        er Se
        Obj        ec        tL        eg        lHold        As        nc pa        ss        es        ", T        es        tS        tatus.NA, 
        D
        ateTime.Now - s        ar        t
        T
        ime, ex.        ess        age,
        

             
         
                  oSt
        g(), args        :         args.        Lo        ();
                                            return;
                        }
        

        p
        tion        ex)
  
         
                     {        
                                  Dow
        (minio
         
        uc        ke        tN        m
        ).Con
        f
        igureAwait(false);
              
         
             new MintL
        o
        g
        ger(nameof(Leg
        a
        lHold
        S
        tatusAsync_Test1), setOb        je        L
        egalHoldS        ig        natur        e,                
          
         
        e
        al        Ho
        dAsync pas
        s
        es", Te        st        Status.FA        IL        ,         Da        eTime        .N        w - s        ta        rt        Time        ,         ex        .
        essage,
                        ex.ToStr
        in        .Log();
                   throw;
                

                      
        ry
   
             {
                           
         
        sing (va
        r
         fi
        e
        tr        eam = rs
        gG        en        e
        r
        ateStre
        ar                   
                       {         
         
         
                    
         
                             v
        a
        r
         pu
        t
        O
        bjectArgs =
        ectAr
        s
        ()
                                 
          
         
        h
        me
        

          
         
                                 
         
                       .        Wi
        t
        hObj        ec        (objec
        Name
        
                           
                        .        WithSt
        mDaa        (f        il
        strea
        m)

         
        le        st
        eam        .L        ength)

         
              
         
                                    .Wi        th        Content        Ty        pe(nu        ll        );

         
         
                   aw        ai        t minio.P        ut        ObjectAsy        nc        (
        eAwait(f        al        se);
 
                }

            var e        ga        lH        ldA
        rgs = new SetObjectLegalHo        ldA
        r
        gs(
        

                 
                  etNa
        m
        )
  
         
         
           
         
         
         
        c
                    
        a
           
           await m
        i
        nio        .S        tO
        b
        je        ctLegalHoldAsync(leg        alHo        ld        Ar        gs)
        .
        C
        nfigureAwait(false);                            
        n
        galHoldStatusAsync_Tes        1), setObje        ct        LegalHo        ld        Si
        nature
        ,
                 
        "T        sts wh
        e
        the
         
        etObjectL
        e
        Te
        s
        tStatus
        .
        AS
        S
        , DateTi
        m
        e
        .
        ow -
         
        tart
        T
        i
        me,
        

         
         
        s:         ar        s
        )
        .
                  catch (NotImplem
        x)        
                    
          {        
                                           new         M        in        Logger(nam
        e
        o
        f(LegalHoldSt        tu        s
        Async
        _
        T
        bj        ega        dSignature,
                    "Tests whether         bjectLeg
        lHoldA
        ync p
        sses
        , TestStatus.NA, DateTime.
        N
        ow - startT
        me, e
        x.        age,
        
               ex
        T
        String()
        ,
         ar
        gs: args).Log
        );
       
        }
                catch
         
        (E
        x
        ception ex)
 
              {
  
         
               await TearDo
        w
        n(
        m
        inio, bucketN
        me).
        o
        fig
        reAwait(fa
        l
        se);
 
         
              
           new MintL        (
        egalHoldStat
        u
        Async_Test
        )
        ,         a
        HoldSignatur
        e
        
         
              "Test
        s whether SetObjectLegal        sses"
         TestStatus.FAIL, D
        a
        teTim
        e
        Now - star
        t
        T
        ime, ex.Messag
        e
        ,
   
         
                    ex.ToString()
         
        args: args).Log();
    
          
             throw;
             
         try
   
         
            {
        

                  
         
        v
        ar getLegalHol
        d
        Args 
        =
         n        Leg
        lHoldArgs(
        )
        
     
         
                  .WithBucket(buck
        e
        t
        ame)
                .WithO
        bj        
            var enabled = await minio.GetObje
        c
        LegalHoldA
        s
        yn
        c
        getLegal
        H
        old
        r
        s).Config
        u
        eA
        w
        ait(fal
        se        ss
        e
        rt.IsTru
        e
        (
        e
        able
        d
        ;
  
         
         
           
         
         
                  = new 
        RemoveObjectArgs()
      
         
                .
        it
        hBucket(buck             
              .W
        i
        thObj
        e
        t(objectNa
        m
        e
        );

          
         
         awai
        t
         m        bje
        tAsync(rmA
        r
        gs).Co
        n
        figureAwait(false);
      
         
         
           new MintLogger(nameof(Le
        ga        _Test1), getObjectLegalHoldSignature,
        
         
                  
        "
        Test
        s
        whether 
        G
        etO
        j
        ctLegalHo
        l
        As
        y
        nc pass
        es        SS
        ,
         DateTim
        e
        .
        N
        w - 
        s
        artT
        i
        m
        e,

         
         
                      a
        rgs: args)
               .Log();
             
        c
        atc
         (NotImple
        e
        ted
        E
        xception ex)
        {
        

         
         
          
         
                  og        Hol
        StatusAsync_T
        s
        1),
        getObjectLega
        l
        Ho         
                "T
        e
        sts whethe
        r         y
        nc passes"
        ,
         TestStatu
        s.        t
        artTime, ex.Me
        s
        sage,
    
                  g
        (), args: args
        )
        .Log();
  
         
             }
        
         i
        on ex)
        
        {
        
   
         
                  gger(
        ameof
        (
        LegalHoldStatu
        s
        Async_Test1),
         
        g
        etObjectLegalH
        o
        ldSig
        n
        at                   wh
        ther GetObjec
        L
        gal
        oldAsync passes", Test
        S
        ta        m
        e.Now - st
        a
        rtTime, ex
        .M         
               ex.
        T
        oString(),
         a        ;
        
            
        t
        hrow
        ;
        
              
        inall
        y
        
        {
            
        a
        wait TearDown
        (
        m
        inio, bucketNa
        m
        e).Co
        n
        fi        lse
        ;
        
        }
        
    }

        #endregion

        #region Bucket Tagging

        internal static async Task BucketTagsAsync_Test1(MinioClient minio)
        {
                       v        ar         
        tartTim        e         =         Da        eT
        m
        w;
                                
        v
        m
        e
        = Get        Rm        a);
        var
        a
        rgs = newc        t
        i
        ng           
          {
      
         
                     {         "        b
        u
        cketName", bucke        tName         }
                  ;
        var tags = ne
        w         g, string>
        {
                   { "        ey1
        ,         value        "
        },
       
         
          
         
        { "key2"
        ,         "v        al
        e
        " },
            
         
                   
         
         { "key
        3
                   };              
         
         t        ry                     
         
         {
 
         
         
                     
         
        tu        nio, 
        k
        et        Na        me).Con
        ig
        u
        l
             
          catch         (        E
        xc        ep        tion
         
        x)
               
        {
        
                                 new M
        i
        ntL        og        ge
        r
        (
        sAs
        nc_Test1),
         
        setBuc
        ke        TagsSignature        ,
                                      
        Tests whether         etBucketTagsA        s
        tus.FA        I
        , DateT
        me.Now - startTime, ex.
        essage
        ,
                  
         
           e
        x
        To        St        ring()        ,         rgs
         
        rgs).        Lo        g(        );

         
          
         
                       th        r
          
         
         try
   
         
         
        
   
         
            
         
         
         va
        r
         
        t
        etBuc
        ketTagsArgs
           
        u
                    
                   .        Wi        thTag        in        (Ta
        ging.GetBucke        Tags(tags
        ))        ;
        i
        nio.SetBuc
        k
        etTag        sA        sy        c(t
        ag        A
        wait(f        alse)
        ;
        
         
         
                  er(
        ameof(B
        c
        etTag
        Async
        _
        Test1),         se        BucketTagsS        ig        nat        ur        ,
                                               T
        e
        s
        ts whet        he         Se        tB        uck
        e
        tTag        sA
        s
        yn        estS        ta        tu
        s
        .PASS,         D        at        eT        me.No        w          
        gs)
        Log();
         
                     }
        catch
         
        (
        ti        n e        x)
    
         
           {
     
         
        e
        r(        na        meof(B        uc        e
        t
        TagsAsync        _T        es        t
        1
        Signa        tu        e,
  
         
                            "Test
        s
         whe        th        er
         
        S
        etB        uc        ketTagsAsyn
        c
         pass
        e
        s
        , D
        teTime.Now
         
        - star
        t
        Tim        e, ex.Message,
         
         
         
            ex.ToString(), args: ar
        g
                    cat        ch        (Excepto        n         ex)
                      {
                                           aw
        it Te        ar
        Down(minio, bucketN        am        ).Config        ur        eA        wa        t
        fl        se        ;
                                
         
        (B        uck
        e
        Tags
        As        u
        cke
        t
        T
        agsSignatur
                               
        "Tests wh        th        r Se        tB        ucketTag
        As
        ync passes"
        Dat
        Time.Now -
         
        startT
        i
        me, ex.Me        sage,
                           
         
                  x.ToString(),         ar        gs: args).Log
        (
        ow;        
            
           }

          
             try
        {
             
              
        va        r         agsArgs =         n
        e
        w 
        G
        tBucketT
        a
        gsA
        g
        ()
               
         
          
         
                    .        With
        B
        
 
         
                          
        v
        a
         tag
        O
        j = a        wa        i
        t m
        i
        n
        i
        TgsAsync(tagsAr
        s
        ).Config        ur
        Aw
        a
        

        rt.
        sNotNull(t
        ag        bj);
                            Assert        .I        sNotNull(ta
        g
        O
        Ge        tTags());
                            v        ar         tags
        Re        gs();
                
          Assert.        re        qual        tagsRes.
        ount, 
        tags.Cou        nt        );

 
         
            
         
            new         M
        i
        ntL
        g
        er(        na        meof(B
        u
        ket        T
        agsAsyn
        c
        Ta
        gs        Si        gnature
        ,
        

                      
         
            
         
         
         "T
        e
        s
        t
        ck        etTag        ssync passe
        tus.PA        S, DateTime
        me        , ar
        s: args).        Lo        g(        );
 
         
             }
                     
         
         
         ca        tc        h (NotImple
        m
        entedE        c
        
           {                 new        tLogger(nameof(BucketTag        nc_Test1
        , getB
        cketT
        gsSi
        nature,
             
         
          "Tests wh
        ther 
        Ge        ketTagsAsync 
        asses", T
        s
        Status.N
        A
        , D
        ateTime.Now -
        startTime,
        e
        .Message,
   
         
          
         
                 ex.T
        Stri
        g
        ), 
        rgs: args)
        .
        Log();
        

              
         }
        c        i
        n ex)
      
         
        {
        
           new Mint
        Logger(nameof
        Buck
        t
        ags
        sync_Test1
        )
        , getB
        u
        ketTag
        sSignature,
         
          "Tes
        t
         whether
        G
        et        y
        c pass
        e
        ", TestS
        a
        tu        T
        me.Now
         
         startTi
        e, ex.Messa
        ge,
                ex.T        rgs: 
        rgs).Log()
        ;
        
    
         
              awai
        t
         
        TearDown(minio
        ,
         buck
        e
        tName).ConfigureAwait(fal
        e
        );
      
          
           throw;
             
         try
     
         
          {
  
         
                 var tagsArgs
         
        =
        new RemoveBucketTagsAr
        gs           .WithBucket(bucketName);
            a
        w
        it minio.R
        e
        move
        B
        cketTags
        A
        syn
        (
        agsArgs).
        C
        nf
        i
        gureAwa
        it          
         
          var ge
        t
        T
        a
        sArg
        s
        = ne
        w
         
        Get
        B
        u
        ck        
    
                   .WithBucket(bucketName)          v
        r tagObj
        =
        awa
        t minio.GetBucket
        T
        ag        g
        s).Configu
        r
        eAwait(fal
        se         
            catch (
        N
        otImple
        m
        entedExceptio
        n
         ex)
        

         
                       
         new 
        M
        intLogger(nameof(B
        u
        cketTags
        A
        s
        ync_Test1), de
        l
        eteBu
        c
        ke        re,
                  
         
             "
        T
        ests whether RemoveBu
        c
        k
        tTagsAsync passes", Te
        st        ime.Now - startTime, ex.Message,
        
         
              ex.T
        o
        Stri
        n
        (), args
        :
         ar
        s
        .Log();
 
         
            
         
        
   
         
         
           
        c
        a
        tch (Exception ex)
      
         
        {
            if (ex.Me
        sa
        ge.Contains(        doe
         not exist
        "
        ))
   
         
                {
           
         
         
          new MintLogger(nameo
        f(        Test1), deleteBucketTagsSignature,
      
         
                  
         
          
         
         "Tests 
        w
        het
        e
         RemoveBu
        c
        et
        T
        agsAsyn
        c         tu
        s
        .PASS, D
        a
        t
        e
        ime.
        N
        w - 
        s
        t
        art
        T
        i
        me,
                     
         
         args: ar
        s)
        
                   g();

                
         
          }
 
         
                 e
        l
        s
        e
            
        {
        
    
         
                   Mi
        tLogger(na
        m
        eof(Bu
        c
        ketTagsAsync_Test1), 
        d
        e
        eteBucketTagsSignature
        ,
            "Tests whether RemoveBucketTagsAsync 
        p
        sses", Tes
        t
        Stat
        u
        .FAIL, D
        a
        teT
        m
        .Now - st
        a
        tT
        i
        me, ex.
        Me          
         
                
        e
        x
        .
        oStr
        i
        g(),
         
        a
        rgs
        :
         
        ar             
                  trow;
            }
              f
        nally
  
         
           
        
            awai
        t
         T        u
        cketName).
        C
        onfigureAw
        a
        it           
         }
   
        }

        #endregion

        #region Object Tagging

        internal static async Task ObjectTagsAsync_Test1(MinioClient minio)
        {
  
         
                             /         Test wi
        l
        l
         
        ru         fo
         file         si
        e
        1KB am
        d
         once
 
         
         
         
         to co
        v
        er singl
        e
         and mu
        lt        part
         p        load
         
        funct
        i
        o
        esL
        st         =         new        Li        t<
        i
        nt> { 
        1
         *         K        , 6 *         MB };
                   eac        h (var size         i
                                                 var startTime
        = DatT        me.
        N
        ow;

         
                         va
        r
        buc        e
        t
        ame 
        =
         
        Get
        R
        a
        n
        ;
         var 
        b
        jectName         =         GetRandoame(10);
          r a
        gs = new D
        i
        ctiona
        r
        y<string, string>
             
                     
                              { 
        "
        Name },
                              { "objectNa
        , obj
        ectNa        e         ,
    
         
                             
                     { "f
        i
        leS
        ze        , size.To
        St        in        (
        ) }
   
         
          
        v
        ar        tags          n        w
         
        tio
        n
        ry<s
        tr        in        g, 
        s
        t
        r
             {
        
         
              {         k
        y1
        "
        }
           
        { "ke        y2", "
        v
        alue2"         ,
                                     { "key3        ",         
        value        3"        }
                    };

         
                               {
                               await Set        p
        Test(        mi        io, bucke        Na        me)
        .
        Conf
        i
        ureAwa        t(
        f
        als
                         
         
        
 
         
                        
                   e
        x
        )
      
         
         
         
          {

         
           
         
         
           
         
                   
        ameof        (
        ectTagsA
        s
        ync_Te        st        ), se        tO        bject
        T
        a
        gsSignature,
 
         
                               
         
         whet
        her SetObje
         pa
        S
        eTi
        e.Now - 
        rt        ime
         ex.Message,
                
         
                  r
        ing(), arg
        s
        : args).Lo
        g
        ()                             t
        row;

         
                                                   

                            v        r         xceptio
        n
        Th        ro        n =         f        alse;
            
         
             
         
         
         {

                                                              us        n
         (v        a
        iles        tr        ea        m = rsg.Gene
        r
        a
        i
        ze))
     
         
                  
        {
        
                    
          var p        tO        ject        r
        s = n
        e
        w PutObjec        tA        rgs()
  
         
                            
         
         
               .        Wi        thBuck
        e
        t(bu        ck
        e
        tName)
    
             
                            .WithObj        ec        t(objectName)
                    
         
         
        amDt        a(        filestrea
        m
        )
    
         
                                                                   .WithOb
        j
        e
        tSi        ze        (files        tr        eam.Length)
    
         
        Wi        th        Cont
        ntType(
        ull);
                                         
         await
         minio        .PutObje
        c
        tA
        s
        nc(putOb
        j
        ect
        r
        s).Config
        u
        eA
        w
        ait(fal
        s
         }
        

        
                                           
         
         
         var         ta        sAr        s          
        new
         
        S
        e
                               
         
          .WithBu
        ke
        t
        )
          
         
          
         
         .W        thO        bj
        e
        ct(objec
        t
        Name)
                     
            .W        it        h
        T
        G
        
  
                  
         
          awai        t
         
        minio.SetObjectTagsAs
        y
        n
        (tagsArgs).ConfigureAwait
        (
        tLogger(        na        meof        Ob        ctTagsAsy        nc        Tes        t1        ), setOb        j
        ctTags
        Signature,
                     
            
         
                                                   
         "        Te        t
         h        et        he        r Se        tO        bj        ec
        t
        us.P
        A
        S,         a
        tT        

           
         
         
         
         
                                       
        g
           
                     catch
         
        (No        tI        mpl
        e
        men        te        dException ex)
   
         
                                      {
                excep        ti                     new MintLogger(nameof(Obj        ec        Tags
        Async_T        es        t1),         se        O
        bjec
        t
        ag        sS        igna        ur        ,
        
  
         
                                                    
         
        "T
        es        ts whet
        h
        ss        es        ",        TestS        ta        u
        s
        .
        N
        Da        teTi        m
        ow -
         
        s
        tar
        t
        T
        i
             
                  x.ToString(), args: ar        s).Lo
                     ca        ch (Exce
        p
        tion 
        e
        )
                 
         
                  {
                                       
         
        excep
        t
        i
         
                            new        tLogger(nameof(ObjectTag        nc_Test1
        , setO
        jectT
        gsSi
        nature,
             
         
              "Test
         whet
        he        tObjectTagsAsync passes", TestStatus.FAIL, DateTime.Now - startTime, ex.Message,
                    ex.ToString(), args: args).Log();
       
                t
        r
        w;

            
         
           
         
         
        

         
          
         
         
         
          
        r
        y
            {
 
         
           
            
          
         exceptio
        nThrown = fa           
             var 
        a
        sArgs = 
        n
        ew 
        Ge        rgs
        )
        
         
                 .Wit
        h
        Bu
        c
        ke        )
 
                  
         
             .WithObj
        e
        ct
        (
        ob           
            
         
           
        var tagObj
         
        = awai
        t
        minio.
        Ge        sy        f
        gureAwait(fa
        l
        e);
      
         
                  o
        Null(tagObj)
        ;
                  
         
                  l
        (tagObj.Ge
        t
        ags(
        )
        );
     
         
         
                  g
        sR        Get
        ags(
        ;
           
                  
         
         Asser
        t
        AreEqu
        al        nt         
              
         
             new
        M
        in        b
        ectTag
        s
        sync_Tes
        1
        ),        g
        ature,
        

                
                  "
        Te        GetO        nc        atus.
        ASS, DateT
        i
        me.No
        w
        - startTim
        e
        ,
        
             
         
             
         
                  )
             
         
         .Log();

          
                            tIm
        lementedEx
        c
        eption
         
        ex)
            {
   
         
         
                  exceptionThr
        ow              new MintLogger(nameof(ObjectTagsAsy
        n
        _Test1), g
        e
        tObj
        e
        tTagsSig
        n
        atu
        e
        
        
         
          
         
               
        "T        ct
        T
        agsAsync
         
        p
        a
        ses"
        ,
        Test
        S
        t
        atu
        s
        .
        NA         star
        tT        ae           
              ex.ToStri
        g
        ), ar
        gs        );
         
          (Exc
        p
        tio
         ex)
     
         
           
         
        {
                exce
        p
        tion
        T
        hr                  gge
        (nameof(Objec
        T
        gsA
        ync_Test1), g
        e
        tO         
                  
         
            "Tests
         w        c
         passes", 
        T
        estStatus.
        FA        T
        ime, ex.Messag
        e
        ,
        
                   
        args: args).Lo
        g
        ();
      
         
              
                   
                  if (e
        x
        cept
        i
        on        {
   
             
         
              await Te
        a
        rDown(minio, 
        b
        u
        cketName).Conf
        i
        gureA
        w
        ai                     
               }
        

           
               try
      
         
                   
        var tagsAr
        g
        s = new Re
        mo         
                  
         
               .Wi
        th         
                   
         
              .
        W
        ithObject(obj
        e
        ctNa
        m
        e
        );         awai
         mini
        o
        .RemoveObjectTagsA
        s
        ync(tags
        A
        r
        gs).ConfigureA
        w
        ait(f
        a
        ls           
        var getTag
        s
        Args =
         
        new GetObjectTagsArgs
        (
        )
                            .W
        it                        .WithObject(objectName);

         
                  
         
           v
        a
         tagObj 
        =
         aw
        i
         minio.Ge
        tO        gs).
        C
        nfig
        ur         
           
         
         
                  No        );
  
         
                    var tagsRes
        = 
        ta        ()          Assert.IsNull
        t
        gsRe
        s)          n
        w MintLogg
        e
        r(name
        o
        f(ObjectTagsAsync_Tes
        t
        1
        , deleteObjectTagsSign
        at                 "Tests whether RemoveObjectTagsA
        s
        nc passes"
        ,
         T
        e
        tStatus.
        P
        ASS
         
        ateTime.N
        o
         -
         
        startTi
        me          
         
           args:
         
        a
        r
        s)
 
         
            
         
         
           
         
         
                                 
         
             catc
         (
        No        Ex                {
     
         
            
                  (na
        eof(Object
        T
        agsAsy
        n
        c_Test1), deleteObjec
        t
        T
        gsSignature,
         
                  her RemoveObjectTagsAsync passes", TestSt
        a
        us.NA, Dat
        e
        Time
        .
        ow - sta
        r
        tTi
        e
         ex.Messa
        g
        ,

         
               
                  g(
        )
        , args: 
        a
        r
        g
        ).Lo
        g
        );
 
         
         
           
         
         
                  catch
         (        )         
            n        meof(ObjectTags
        s
        nc_Te
        st        Tag
        Signatur
        ,
           
                        "
        T
        es        e
        ctTagsAsyn
        c
         passes", 
        Te        i
        me.Now - s
        t
        artTime, e
        x
        .M           
              
         
        x.ToS
        ring(
        )
        , args: args).Log(
        )
        ;
      
         
         
                throw;
        

             
         
                    fina
        l
        ly
      
         
             {
        

                  ait Te
        a
        rDown(min
        i
        o, buc
        k
        etName)
        .
        C
        o
        nf        );

               
         
         }
   
         
            }
 
         
         
         }

        #endregion

        #region Object Versioning

        internal static async Task ObjectVersioningAsync_Test1(MinioClient minio)
        {
          
         
            // Test will         un tw
        i
        d         once

               
        / for 6MB to cover
        single
         and multipart         plo
        a
         funct        io        ns
        

                              
        var sizes        Li        s
        * MB
         
        ;
  
         
        i
        n s
        i
        z
        e
        {
        var l
        o
        pIndex        = 1;
                            va
        ta
        rt        i
        var bucketNam        e =
        G
        tRan
        do           
           var obj
        e
        ctName
         
        = GetRandomNam        e(        0);
  
         
         
                               va        r         rgs = new         D        icti        n
            {
                { "        bu        cketName
        bu        ck        tNa
        me         }        
                                                                { 
        "
        bjectNam
        e
        ", 
        b
        ectNa        me },
        

          
         
               
         
        .T
        o
        String(         }         
         
                     
         
          };
        

         
           
         
         
                                 
         
                    aw        ait         tu
        p
        et        wait(        fa        lse);        
              
         
            
                     
           // Set         e
        rsioni
        n
        g enab        ed        test                 
         
         
                                var setVersionin
        g
        Args()
               
                       .Wit        Bucket
        (buc        ke        tName)
  
                      
         
                
         
           
         
        WithVer        sio
        n
        ng
        E
        nabled(        );        

        ai
        t
         minio.S
        e
        t
        Vr        sion
        i
        gAsy
        n
        c
        (se        tV        e
        r
        s
        eAwai
        t(         
         /
         
        Pu        t         the s        am        e object
         t         
             
                                  
         
        using
         v        ar         f        ilestrea
        m
         
        = rsg        .G        enerat        eS        tre
        a
        mFrom
        S
        ee              
         
         
        var
        =
        ()

                                  
         
           
                          .WithBucket
        (
        b
         
                           
         
          .WithObj
        e
                                     
         
                     .        WithS        tr        ea        m
        Da                                             
                    .WithO        bj        ect
        S
        ize(f        il        es        tr
        e
        a
        m.Length)
                      
         
                               
         
         
        ont
        ntType(        nu        ll);
         
           
                        aw        ai        t         p
        ut        ObjectArg
        s
        ).Configur
        eA         
                  
         
         }

      
         
         
        r         fi        stream=         r        g.G        ne        teStr
        e
        amF        romSeed(size))
 
         
                   
         
                             {
                  
         
             
         
         
        gs = n
        e
        w PutObje
        c
        tArgs(
        )
        

           
             .Wi        hu        cket(b
        u
        cketNam
        e
        )
        
               
         
            .Wi        hO        ject(        o
        b
        je                   
                                    
         
          .Wi        th        S
        t
        reamData(filestream)        
                    
                                      .Wit
        hO        gth)
                                    .W        th
        ontent
        Ty        pe        nu        ll        ;
     
                               
               
        a
        wa        it        i
        .PutObje        ct
        A
        reAw
        a
        fa        ls        )
        ;
         
           
         
         
                   
         will        e         2 more versions of the 
        bj
        ec         
        ers
        onCount = 
        l
        oop        nde
        x
         * 2;
                                                   
         
        a
        ait ListObje        ct        s_        est(minio, 
        b
        ersionCount, true, true).ConfigureAwait(fals
        e
        ;

                        
         
          
         
                              new M        in        Lo
        g
        r(na        me        of(O        je        Ve
        r
        sionin        gA        y
        Si
        g
        nature,

                             
            
                                                 
         
          "        Te        t
        s
        in        rsioni        g
        Async/Rem
        ve
        Ve        c            
                  
         
          Test
        S
        tatus.PASS,                                   
         
         
                 DateTim        .Now - st        r
        );        

            
               
               // G        et Versioni        ng        Test
 
                              
         
            
        v
        r getVe        rs
        i
        oni
        g
        rg        s         = new G
        e
        Ve        rs        i
        oningAr
        g
         
         
         .Wi        hBuc
        ke        t(        b
        cket
        N
        me);
        

         
           
                             
        oning
        C
        io        ngAsy        nc        (g
        sC        lse);
                                  
         
                               
         
        Asse        rt        .IsNot
        Nu        ll        (v        er        sioningConfi
        g
        );
  
         
         
        A
        N
        ion        onfig.Status                           Assert.I        e(versio
        ingCon
        ig.St
        tus.
        quals("enabled", StringComp
        a
        rison.Ordin
        lIgno
        re        ));

                    new MintLogger(nameof(ObjectVersioningAsync_Test1), getVersioningSignature,
                        "Tests whether Se
        Versionin
        A
        ync
        GetV
        e
        rsi
        o
        i
        g
        s
        nc
        /
        e
        o
        eV
        r
        sioningAsync pass
        s
        ",

            
          
                 
              TestSt           
                 
         
         
                  .No
         - startT
        m
        , args: 
        a
        rgs
        ).           
                  
         
        / Suspend Ver
        s
        io
        n
        in           
                  
        s
        tVersioningAr
        g
        s 
        =
         n        nin
        Args
        )
           
                  
         
              
         
         .With
        Bu        am         
                 .Wi
        t
        Versioning
        u
        sp         
                   a
        w
        it minio.S
        t
        Ve        t
        ersioningA
        r
        s).C
        o
        nfigureA
        w
        a
        t(         
                    va        t              
           await L
        i
        stObj
        e
        ts_Test(mi
        n
        i
        o, bucketName,
         
        "", o
        b
        je        Co        

                    new MintL        ers
        oningAsync_Test1)
         
        emo
        eVersioningSignat
        u
        re         
         "Tests wh
        e
        ther SetVe
        rs        g
        Async/RemoveVersionin
        g
        A
        sy             
             
         
            TestStatus.PAS
        S
        ,
               
         
         
               DateTim
        e
        .Now 
        -
         st        .Log();
                }
            }
            ca        xcept
        o
        n e
        )
        
         
         {

         
                       new Min
        t
        Logg
        e
        r(        in        nin
        Signature,
  
         
           
                   "T
        e
        st        /
        GetVersion
        i
        ngAsync/Re
        mo        e
        stStatus.N
        A
        ,
        
                  r
        tTime, ex.Mess
        a
        ge, ex.ToS
        tr         
                 }
   
         
                ca
        t
        ch (Ex
        ce         
                   new 
        M
        intL
        o
        gg        ngAsy
        c_Tes
        t
        1), setVersion
        i
        ngSignature,

         
         
                      
         
           "T
        e
        st        ig        sync/
        e
        mov
        Versioning
        s
        nc 
        p
        asses", TestStatus.FAI
        L
        ,
  
         
                  im        ssa
        e, ex.ToStrin
        (
        , a
        gs: args).Log
        (
        );         
               }
 
         
                  
        fi         
               awa
        i
        t TearDown
        (m        w
        ait(false);
  
         
                 }
        
        }
    }

        #endregion

        #region Object Lock Configuration

        internal static async Task ObjectLockConfigurationAsync_Test1(MinioClient minio)
        {

         
            
        var star
        Ti
        e =
        teT        ime
        cket        ame = G
        ta        nd        omName(1        5)        

         
         
        et        and
        mName(10);
     
         
          var
         r        g         onary<strin
        g
         str
        i
        g>
 
         
         
             {
       
         
            {         "        u
           
          };
                     
         
        var se
        t
        LockNotIm        pl        emented = s        ar         ge        tLockN
        o
          try
                          
         
         await Setup_W        ithLo
        c
        k_Te        t(m        inio, bucketNam
        ).Conf
        ig
         //TO        DO        : Use
         
        it f
        o
             {
 
         
           
         
                     t
        i

        Configuration(DateTime
                   
              us        in        g (v        r         ile
        t
        eam
        =         rs        .Gener        ateStrea        m
         
        * KB))
            
         
                                         {

         
         
        , for         2s          
         
                         v
        a
        ru        c
        t
        A
        rgs1 = new Put
        O
        bject
        A
        r
           .Wi
        t
        hBucke        t(bu
        c
        ketName)
                          
         
        .t        eo        jectName)
  
         
              
         
                  eamDat
        a
        (files
        t
        ream)
                          
         
                      
                   .Wit
        h
        ObjectSiz
        e
        filestream.        Le        ngth)
        

                                          
         
         
                  ura
        ion(object
        R
        etenti
        o
        n)
                        
        .
        W
        thContentType(null);
 
                  inio.PutObjectAsyn        (putObject        rg        1).C        nfigu        re        Awai        t(        al        e)        
                                                    }        
                     
          usi        g         (v
        treamFromS
        e
        ed(1
         
                
         
                    
        var put        bj        c
        Args
        2=         n        ew P        u
        t
        Obj
        e
        c
        t
         
           .Wit
        Buck        et        (bu        ck        etN
        me)

         .        ithObject(obje        ct        Nm        e)           
                                          .
         
                           
         
               .Wi
        t
        g
        th)        
                                             
         
        .
        W
        io        n(        bj
        ctRet
        e
        nt        on        )
             
         
                         .WithC        on        e
        n
        t
        Ty        pe        (null);
                              
         
             
         
         
        ect
        sync(putObj
        c
        A
        r
        lse);
                                                       }
  
         
                                        
         
        
        }
        

          
         
         c        at        ch (NotIm
        p
        ement
        e
        d
        Exception ex)

         
             
         
         
        Log
        er(nameof(
        O
        bjectL
        o
        ckConfigura        ionAsync_Test1),
         
        ject        ockConfigurationSi        gn        at
        u
         whether S        et        Obje
        tLockC        onfigurationA
        s
        ync passes", T        estSt
        a
        tus.NA, DateTime        .N        ow         - s
        artTim
        e,
         ex.ToStri
        n
        g(),         a        r
         await T        ea        r
        Dow(        mi        io, bucke
        t
        ame)
        .
        onfi
        g
        u
        reA
        w
        a
        it                               ca
        c
        h (Exception ex)
                       
        
          
        n
        ctL
        ckCo        nf        igura        ti        o
        n
        Async_
        T
        est1), setObjectLockConfigu
        r
        a
        ionSignature,                 
         
        Obj        ec        tLo
        kConfig
        rationAsync passes
        ",         TestStatus        .F        AIL, Da
        t
        eTi        me.Now - startTime,
              
                          ex.M
        e
        ss        ag        e
         args).L
        o
        g()
        

                          
         
        wa
        i
        t Tea        rD        ow
        n
        mi
        n
        io,         bucke
        t
        N
        a
        e).C
        o
        figu
        r
        e
        Awa
        i
        t
        (
         
                             }

         
                       try
          
                   
        kArs         =         new         Se        tObj
        e
        ctLock
        C
        onfigurationArgs()
                                  
         
         
          .WithBuck        et        (bucketName
        )
        Con        fi        gur
        tion(
 
                          
        n
        ew ObjectLockConfi
        gu        at        ion(        etentionMod        .G        VEN        AN        CE, 3
        3)
                        )        ;
        o.SetObj
        e
        ctL
        c
        Configura
        t
        on
        A
        sync(o        j
        e
        tL
        o
        ckArgs).        on        i
        ur        Aw
        a
        t(fa
        l
        s
        e);
                   
                  ogger
        (
        o
        sync_Tes        c
        Sign        at        u
        e,
               
             
         
          "Tests w
        h
        e
        ther SetObje        ctL
        o
        ckCon
        f
        i
        ases", TestS        s.P         DateTime.No        startTime,
                args: ar        Log();
 
              
        
    
           c
        tch (NotImplementedException ex)
 
         
              {
   
             
                  LockNotImplem
        nted = tr
        e
        
       
         
           
         new MintLogg
        r(nameof(O
        j
        ctLockConfigu
        r
        at
        i
        onAsync_Test1
        , setObjec
        L
        ckConfigurati
        o
        nS
        i
        gnature,
    
            
         
           
        "Tests whe
        t
        her Se
        t
        bjectL
        ockConfigura        s
        es", TestSta
        t
        s.NA, Date
        ime.Now - s
        tartTime,
   
                    ex.Messag
        ,
        ex.To
        String(), arg
        : args).Log();
      
         
        
    
           catch (Exception ex)
             
            new MintLogger(
        n
        ameof
        (
        bjectLockC
        o
        n
        figurationAsyn
        c
        _Test
        1
        ),        ckConfigurationSignature,
                    he        ckC
        nfigurationAsyn
         
        ass
        s", TestStatus.FAIL, DateTim
        e
        .Now - s
        t
        artTi
        m
        e,
    
         
         
         
         
                  , ex.
        o
        Str
        ng(), args
         
        rgs
        )
        .Log();
            aw
        a
        i
         
        ea
        r
        Do        Na        alse);
            throw;
        ry

               {
     
         
           
        var objectLoc
        k
        Ar        n
        figuration
        A
        rgs()
    
                  c
        ketName);

         
                  
         v        G
        etObjectLockCo
        n
        figuration
        As        i
        gureAwait(fals
        e
        );
       
         
            As
        se         
                 Assert.IsTrue(con
        f
        ig.ObjectLockEn
        ab        o
        nfiguration.Loc
        k
        Enab
        l
        ed        rt.Is
        otNul
        l
        (config.Rule);
        

                    As
        s
        e
        rt.IsNotNull(c
        o
        nfig.
        R
        ul        o)        sert.
        r
        eEq
        al(config.
        u
        e.D
        e
        faultRetention.Days, 3
        3
        )
        

          
         
                  gg        Con
        igurationAsync
        T
        st1
        , getObjectLo
        c
        kC         
                  
         
           "Tests 
        wh        g
        urationAsy
        n
        c passes",
         T        .
        Now - startTim
        e
        ,
        
                  

                }
    
         
           catch (
        N
        otImpl
        em         
          {
            getLockNot
        I
        mplemented = tr
        ue        g
        ger(nameof(Obje
        c
        tLoc
        k
        Co        t1), 
        etObj
        e
        ctLockConfigur
        a
        tionSignature,
        

         
                      
         
        "Test
        s
         w        Lo        ionAsync passes", TestSta
        u
        s.NA, DateTime.Now - st
        rt
        Time,
              .Me
        sage, ex.T
        o
        String
        (
        ), args: args).Log();
        }
  
         
         
           catch (Exception ex)
        {
 
                  earDown(minio, bucketName).ConfigureAwait(false);
    
         
              new 
        M
        in
        t
        ogger(na
        m
        eof
        O
        jectLockC
        on        Te
        s
        t1), ge
        t
        bj
        e
        ctLockCo
        n
        f
        i
        urat
        i
        nSig
        n
        a
        tur
        e
        ,
        
            "T
        sts whet
        h
        er Ge
        t
        bjectLockC
        o
        n
        figurationAsyn
        c
         pass
        e
        s"        .FAIL,
         DateTime.Now - startTime
        

                 
          
            ex.Messa        ing
        ), args: a
        r
        gs).Lo
        g
        ();
            throw;
        }


         
         
             try
        {
            if (
        se        ted || getLockNotImplemented)
            {
          
         
            // Can
        n
        ot t
        e
        t Remove
         
        Obj
        c
         Lock wit
        h          L
        o
        ck impl
        e
        en
        t
        ed.
    
         
         
         
            
         
          ne
        w
         
        Min
        t
        L
        og        bject
        ockConfi
        g
        urati
        o
        Async_Test
        1
        )
        , deleteObject
        L
        ockCo
        n
        fi        ature
        ,
                   "Tests whethe        ctL
        ckConfiguratio
        A
        ync
        passes", TestStatus.NA, DateTi
        m
        e.        

                  
         
                 "
        Fu         
        is not implemented", 
        ""        
  
                     await Tear
        D
        own(minio, bu
        c
        ketName).C
        o
        fi
        gu         
                   retu
        n;
  
         
                 }

            var obj
        e
        ctLockArgs = n
        e
        w
         RemoveObjectL
        o
        ckCon
        f
        ig        )
 
                  
         
           .Wi
        t
        hBucket(bucketName);
            a
        w
        a
        t minio.RemoveObjectLockConfigurati
        on        Args).ConfigureAwait(false);
            var getObject
        L
        ckArgs = n
        e
        w Ge
        t
        bjectLoc
        k
        Con
        i
        urationAr
        gs           .
        W
        thBu
        c
        k
        et(
        b
        u
        cketName);
            va
         
        config = await minio.Ge
        Ob
        jectLockConf        nc(getObjectLockArgs)
        C
        nfig
        ur        );

                  
         
        Assert
        .
        IsNotNull(config);
            Ass
        e
        r
        .IsNull(config.Rule);
            n
        ew        of(ObjectLockConfigurationAsync_Test1), deleteObjectLo
        c
        Configurat
        i
        on
        S
        gnature,
        

           
         
                 
         "        mo
        v
        eObject
        L
        ck
        C
        onfigura
        t
        i
        o
        Asyn
        c
        pass
        e
        s
        ", 
        T
        e
        stStatus.PASS, DateTime.N
        w
         - startT
        me
        ,
                  arg
        ).Log();
 
         
              
        }
        
        catch (NotImplementedExce
        p
        t
        on ex)
        {
            new Mi
        nt        jectLockConfigurationAsync_Test1), deleteObjectLockCon
        f
        gurationSi
        g
        natu
        r
        ,
      
         
           
         
           "Tests
         w        ec
        t
        LockCon
        f
        gu
        r
        ationAsy
        n
        c
         
        asse
        s
        , Te
        s
        t
        Sta
        t
        u
        s.        .Now 
         startTi
        m
        e,
  
         
                  
         
         
        ex.Message, ex
        .
        ToStr
        i
        ng        gs).L
        og();
       }
        catch (Exce           
          {
          
         
        ew 
        intLogger(nameof(ObjectLockCon
        f
        ig        t
        1), delete
        O
        bjectLockC
        o
        nf        nat
        re,
  
         
             
             
        "
        Tests whether RemoveObjectLockC
        o
        nfigurationAsy
        n
        c
         passes", Test
        S
        tatus
        .
        FA        .Now -
         
        startTime
        ,
        
     
         
                  essage
        ,
         ex.To
        S
        tring(
        )
        , args: args).Log
        (
        );
     
         
              throw;
        }

         
               fina
        l
        l
        y
              
         
           await 
        T
        ask.De
        l
        ay(1
        5
        00        wait(f
        a
        lse);
   
         
              
         
         awa
        i
        t TearDown(minio
        ,
         b        onfigu
        r
        eAwait(f
        a
        lse);

         
            
         
          }
    }

        #endregion

        #region Object Retention

        internal static async Task ObjectRetentionAsync_Test1(MinioClient minio)
        {
        var         startTime =         D        a
        eTime.
        Now        
        v
        a
        r bu
        c
        etName =
         G        etR
        n
        omName(15        );        

        Name
         
         Get
        R
        a
        ndo
        m
        O
        bectName(10
        var a
        gs         =         new Di        ctionary<string,
        st
        r
                     { "bucke        tN        ame", buck
        t
        ame         }
        ,
        { "
        bjectNam        e",
         
        object
        N
        ame }        
                                };

        try
        
        {
        

                                 await Setup_Wi        th        Lock_Test(min
        i
        reAwai        t(        alse);

               }
        catch (NotImpl
        mented
        Exception e        )
         
         
          
         
          {
    
         
           
         
         aw        it         Tear
        Do        Na
        m
        e).        onfi
        g
        re
        A
        wa        it        (false)        ;
         
         
            
         
           n
        e
        w
         Mi
        n
        t
        Logger(name
        entio
        A
        sync_Test
        ),
         setObjectR
        ure,

                
         
             
         
        Tests whet
        h
        e
        r SetObjectRet
        e
        ntion
        A
        s
        stSa        tu        s.N        A,        DateT
        i
        me.        No        w -         ta        tTime, ex.        Message,
                                
        e
        x
        ToS        tr        ng(        ),         args: ar        gs        ).        Lo        ();
                                           r        etur        n;        
         cat        h (
        xcept        io        n 
        x)
                       {
                                    await TearD        ow        (mini        o,         
        bucketName).Configur        Await(fa
        l
        se)
        

                 
        f(
        O
        bjectRe
        t
        nt
        i
        onAsync_T        s
        t
        ), s
        e
        Ob        je        ct
        R
        e
        ten        ti        o
        n
        Si             
                "Te
        etObjectRetent        se
        ",         TestStatus.FAIL, Dat
        Ti
        e.Now - startTime, ex
        .M         
        ing(),         ar        gs
         a        rg        )
        Log();
                               thr
        w;
          
           
         
        

 
              
        ry        
           
            {        
                                
         
                    
           u        si        ng         (var
         
        filest
        r
        eam = r        g.Generat        eS        treamF        ro        Seed(1 * KB        ))        
          
                 {
                                                var putObjectArg        s         )
                    .WithBu        ket(bucketName)                                    
              .WithObject(objectN        am        e
        )
        
  
         
                 
         
        es        tream)
      
                       
           
        .WithObject
        Size        (f
        i
        estr
        e
        m.Le
        n
        g
        th)
        

         
         
        .With
        onten        tT        ype
        (
        null)
        ;
                                    
         
         
                  aw        it         minio.Put
        O
        bject
        A
        s
        .Conf        g
        u
        
         
  
                                 var unt
        l
        at        e         =         ateTime.Now.Ad        dD        ays(pl        us        Days);
      
         
         
        A
        rgs         =         new Se
        t
        ObjectRete
        n
        ti                                       
            .
        W
        ithBucket(bucketName)
                     
         
          .WithObject(
        o
        b
        jectName)
    
         
             
         
                  nti
        nMode(Rete        nt        onMode
        G
        VE        RN        A
        CE)        
                                       .        it        RetentionU        nt
        i
        lD                                                   aw        ai
        t         mi        ni        o.SetObj
        e
        ct        nc
        Retent
        o
        Args)
        Confi
        g
        ure        wai        t(false);
            new 
        M
        intLogger(nameof(
        Ob        ec        Reten        tionAsyn
        c
        _Test
        1
        )
        tionSig        na        ture,
   
         
              
         
                  s wh        et        he         
        SetObj
        e
        ctRete
        n
        tion
        A
        sy        Te        st
        tatus.PASS
        ,
         DateT
        i
        me.Now - startTime,
              
         
         
           args: args)
                                               .Lo        g(        ;
     
         
        ImplementedException         e        x)
                       {
                                    ne        w         Mi        tLogg        er        nameo        f(        Ob        ject        Re        te        ntionAsy
        n
        c_Te
        s
        1), setO
        b
        je        tR        t
        ntionSi        nat        ur           "
        T
        sts         h
        e
        the
        r
         
        S
        nionAsync passe
        "
        , TestStatus.NA, Da        te        Ti        e.
        ow
         - startTime        ,
 
                           
         
          ex.T
        o
        String(), args: args).Log();
     
         
         
        }
        catch (Exc        ep        ion ex)
                        {
                   
        n(minio         bucket
        ame).Configu        re        Await(fa        ls        );
                                    n        e
        ntLog        ge        (
        ame        of        (O        bjec        tR        et        e
        n
        ti
        o
        Async_Te
        s
        t1)
        tObjectR        et        e
        n
          
         
              "
        T
        sts         w        hether         S        et
        O
        b
        j
        ctRe
        t
        ntio
        n
        A
        sy        nc
         
        p
        asses", TestStatus.FAIL, 
        a
        teTime.No
         -
         
         
          
               ex
        .
        ToStri
        n
        g(), args: arg        s)        .Log();
                    t
        h
        r
        w;
                        }

                        try
        {
              
         
        rg        s = n
        w GetOb
        ectRetentionArgs()        
                                .W        it        Bucket
        (bucketN        am        e)
                         
         
             .Wi
        t
        hOb
        e
        objectName        )
        nf
        i
        g = awai        t         in
        i
        o.G        et        Objec
        tR        et        e
        tion
        A
        ync(
        g
        e
        tRet        en        t
        i
        reAwa
        it(false);

         var         pl        us        Days = 10.0
        ssertI        sN        otN
        u
        ll(        co        nf
        i
        g)        ;
          
         
         
                Assert
        .
        Ar        eE        qua
        l
        (
        ntio        nM        o
        e.GOVERN
        A
        NCE);
                           v
        a
        r
         untilDa        te         = Dat
        e
        Ti        me        .Pa
        r
        s
        in        lDa        null, DateTi        yles.RoundtripKind);
                Asser
        .AreEq
        al(Ma
        h.Ce
        ling((untilDate - DateTime
        .
        Now).TotalD
        ys), 
        pl        ys);
        
           new Mi
        t
        ogger(na
        m
        eof
        (ObjectRetent
        onAsync_Te
        t
        ), getObjectR
        e
        te
        n
        tionSignature
        
         
         
                "Tests whet
        h
        er
         
        GetObjectRete
        tion
        s
        nc 
        asses", Te
        s
        tStatu
        s
        PASS, 
        DateTime.Now        ,
                    
         
              args
         
        ar         
              .Log()
        ;
                }

               catc
        h(NotImplementedExceptio          {
 
                  new MintL
        o
        gger(
        n
        meof(Objec
        t
        R
        etentionAsync_
        T
        est1)
        ,
         getObjectRetentionSignat
        r
        e,
                "Tes
        s 
        whether GetO        onAsy
        c passes
        "
        , Tes
        t
        tatus.NA, 
        D
        a
        teTime.Now - s
        t
        artTi
        m
        e,        
  
                  
         
          ex.T
        o
        String(), args: args).Log(
        )
        ;
                }
        catch (Ex
        ce          {
            await TearDown(minio, bucketNa
        m
        ).Configur
        e
        Aw
        a
        t(false)
        ;
        
  
         
               ne
        w
        Mi
        n
        tLogger
        (n        nt
        i
        onAsync_
        T
        e
        s
        1), 
        g
        tObj
        e
        c
        tRe
        t
        e
        nt        ,
    
                    "Tests whethe
         
        GetObject
        et
        entionAsync         tStat
        s.FAIL, 
        D
        ateTi
        m
        .Now - sta
        r
        t
        Time, ex.Messa
        g
        e,
  
         
                  x.T
        String(), 
        a
        rgs: a
        r
        gs).Log();
            thr
        o
        w
        
        }

        try
   
                   var clearRetentionArgs = new ClearObjectReten
        t
        onArgs()
 
         
            
         
                
        .
        Wit
        B
        cket(buck
        e
        Na
        m
        e)
    
                  bj
        e
        ct(objec
        t
        N
        a
        e);

         
            
         
         
           
         
        a
        wa        arObj
        ectRetentioAsync(clearRetentionArg        Awa
        t(false)
        

          
                  etRet
        n
        tio
        Args = new
        G
        tOb
        j
        ectRetentionArgs()
   
         
         
         
          
         
                  et           
                 .Wit
        O
        jec
        (objectName);
        

                   
        = await mi
        n
        io.GetObje
        ct        t
        entionArgs
        )
        .Configure
        Aw        }
        
        catch
         
        (NotImplem
        en         
            {
        
         
           new Min
        t
        Logger
        (n        n
        Async_Test1), c
        l
        earO
        b
        je        ture,
             
         
                  "Tes
        t
        s whether Cle
        a
        r
        ObjectRetentio
        n
        Async
         
        pa        tt        ime
        Now - sta
        t
        ime, ex.
        M
        ess
        a
        ge,
   
         
                
         
                  (),
        args: args).Log(
        ;
           
            }
        catch (E
        x
        ce         
         {
       
         
            var er
        rM        a
        ge.Contain
        s
        ("The spec
        if        n
        ot have a ObjectL
        o
        ck configurat
        i
        on");
    
                  L
        ock)
            {
   
         
                 
         
                  er(na
        eof(O
        b
        jectRetentionAsync_Test
        1
        ), clearObjectRe
        t
        e
        ntionSignature
        ,
        
    
         
                   "T
        sts whethe
        r
         Clear
        O
        bjectRetentionAsync passes
        "
        ,
        TestStatus.PASS, DateTime.N
        ow                      args: args).Log();
            }
        

                  
         
        else
        

                
         
          {
         
                 
                  meof
        (
        bjec
        tR        s
        t1)
        ,
         
        clearObjectRetentionSigna
        u
        re,
                   
        "T
        ests whether        Ret
        ntionAsync
         
        passes
        "
        , TestStatus.FAIL, DateTim
        e
        .
        ow - startTime,
           
                  e, ex.ToString(), args: args).Log();
         
         
             await
         
        Te
        a
        Down(min
        i
        o, 
        u
        ketName).
        C
        nf
        i
        gureAwa
        it          
         
              th
        r
        o
        w
        
   
         
            
         
         
         }

         
         
              }

        try
    
         
          {
     
          
            var rmAr        oveOb
        ectArgs(
        )
        
    
         
                  
        .
        W
        ithBucket(buck
        e
        tName
        )
        
            
        WithObject
        (
        object
        N
        ame);

            await m
        i
        n
        o.RemoveObjectAsync(rmArgs)
        .C        lse);
            await TearDown(minio, bucket
        N
        me).Config
        u
        reAw
        a
        t(false)
        ;
        
  
         
           }
    
         
          
        c
        atch (E
        xc          
         
        {
      
         
         
         
          ne
        w
        Mint
        L
        o
        gge
        r
        (
        na        etent
        ionAsync_Tet1), clearObjectRetenti        
  
                     "Te
        r
        own
        operation ClearObjectR
        e
        te        t
        Status.FAI
        L
        , DateTime
        .N        e
        x.Message,
        

                  
         
                  ng(
        , args
         
        rgs).
        og();
        

                    throw;
    
         
           }
    }

        #endregion

        internal static MemoryStream CreateZipFile(string prefix, int nFiles)
        {
        

             // Creat
        ZipFile 
        r
        ates          file, popul        at        es
         
        it wi        th         
        <
        n
            //         sm        all file        s,                  refixed         i
        t
        h <pref        ix        > a
        n
        d         e         pl        us        a single

         
                     /        / 1M
        B
         fi        le        .g        erates and 
        r
        turn
        s
        a memory strea
        m
         of the zip f
        i
        le         The n        m
        es of         he
        s
        e f        l
        e
        s a        e ar
        r
        a
        nged in "
        f
        le-size>.        bi        n" 
        f
        o
        rmat,
           
         
                  // like 
        "
        1
        ate
         as        a         mall 
        b
        ina        ry         fil        e
         in 127 bytes        size.
                       a
        r
        outputMemSt        re        am         =         new MemoryStr
        zipStream =         ne        w 
        OutputStream(o        tputM        mS        re
        ;

  
                      zi        pStream        .S        tLev
        e(        3); //0-
        9
        , 9
        b
        ing the h
        i
        on
 
           b
        y
        

        
  
         
                            yCreateDirector        y
        p
        refix);
        for (va
         i
         = 1; i <= 
           
           {
                       
                            // M
        a
        ke a s        in        gl        e 1Mb file
                          
                  if        (i == nFiles) i = 1000000        

         
                  eName        =p        refix +
        i + ".bin";
                   
        ar newE        nt        r
         = n        ew        ZipEn
        t
        ry
        (i        le        Na        me)
  
         
           
         
                   {        
                        
         
          
         
           Date
        T
          
         
                                };
        

         
         
            
         
           z
        i
        p
        Str
        ea        m.        Pu        y(newEntry);

 
         
                         u
        in
        g var stream        ateSt
        eamFromS
        e
        ed(i)
        ;
                                   b
        y
        tes = str        ea        m.T        oA        r
        a
        y();

                   
        ar         in        tre        am         = new M        e
        morySt
        r
        eam(bytes);
                                    St        re        am        t
        i
        l.        Co        y(inS        tr        ea        m, z        ip        St        ream, new byte
        [
              inStrea
        ose();
                           zipStr
        am.Clo
        seEntry();
          
         
            
        }
                                //        S
        ett
        n
         own        er        sh        ip t
        o
        Fa
        l
        se kee        ps         
        t
        m 
        o
        pe        
     
         
         
         i        pS        tre
        a
        .I        sS        tr
        e
        a
        mOw
        n
        e
        r                      /        /
        hthe ZipOutpu
        fo        put
        emStream
        z
        p
        tre
        m.Close();

                       output
        M
        e
                               o        tput
        M
        emStr        ea        .Se        ek        (
        0
        )
        ;

                        r
        e
        turn o        ut        pu        tM        m
        S
        tream;
    }

        internal static async Task GetObjectS3Zip_Test1(MinioClient minio)
        {
            
         
          var
         
        l/"
        
               v        ar s        artT
        m
         = D        te        ime.        No        w;
        var b
        u
        c
        (
        15);
     
         
          var rand
        o
        d
        omNa        e(        5) +        "
        .zip";
   
         
                  me 
         GetRad        o
        Objec
        Nam        e(        15
        )
         + ".zip        ";

        var 
        a
        rgs = n        ew Dictiona        ry        <
        string, st        ring>
        

             
         
          {
           { "bucketN
        "
        , bucketNa        me },
        
          
         
        a
         }

                               };
            
            tr
        y
        
        {
                    awai
        t
         
        et        up        _Test        (m        inio, bucketName).Conf
        i
                                
         const 
        nt n        Fi        le        s = 500;
                            u
        g var 
        memStream = CreateZipFile(p        th,
        n
        iles        ;
           
         
          
                             var         p        ut
        O
        je
        c
        tArgs()

         
         
                      
         
            
         
        .
        Wit
        hB        c
        ame)
          
         
                            .WithOb
        ec
        t
        e
           
        Wit        hStreamD
        t
        (m
        e
        mStrea        m)
        

                                   
             .With        Objec
        Size(m
        mStr
        am        .L
        ngth
        ;
                                    
        wait minio.        Pu        tO
        bje
        ec
        A
        rgs).Con        fig
        ur        )
        tra
        tHeader = 
        n
        ew         ict
        i
        on        ar        y<string, string>
                         
         
         {
                                        { "x-minio-ext        r
                   ;


                                  // Ge        Ob        ject api test
     
              v        ar         r        = new Rand
        o
        m();
        

                
         
                  va
         
        ingleFile
        Na        s - 
        1
         + "
        .
        b
        in"
        ;
        

                  r
         = o
         
        e;

                  
         
        // File         n        mes i        n the zip file also         sh
        ow         
         sizes of the files
                          
         
        5.b        in" 
        as a si
        e of 35Bytes
                   v
        expecte        dF        i
        eSize = Pa
        t
        h.Ge
        t
        ileNameW        it        ho        tE
        t
        nsion(sin
        g
          
         
        var get
        O
        je
        c
        tArgs = 
        n
        e
        w
        GetO
        b
        ectA
        r
        g
        s()
        

         
         
        Bucke
        uck        et        ame)
        

                               
         
                    .With
        F
        il        e(        and        om        FileName        )

         
             
         
         
        (si        ng        le        O
         
        eders(extract
         
        esp
        = awai
         
        ini
        .GetObjec        Async(g
        et        O
        wa        it        (false);

         
                                   //         
        he file fro        m         the ret        ur        ned
         
        if          Ass
        rt        .A        re        q
        u
        al(expectedFileSi
        z
        e, res
        p.        S
        ize.ToString()
        )
        ;

  
         
                  adObj
        ct api t
        e
        st
  
         
                        v        r 
        s
        t
        atArgs         =         new S        ta        O
        b
        jectA
        rg        s
         
        uc        ket(
        u
        ck        etName)

          
         
         
        eOb        je        tName        )
                                                        
        .
        WithHeaders(e        xtractHeader);
        

         
                  var stat = await mi
        n
        (sta        Ar        s).C        nfigureAw
        it(false);
                            // V
        erify th        e size
         
        of t        he         
        ile from         t
        he 
        e
        urned inf
        o
                   
         
               
         
        te
        d
        FileSize
        ,         e
        p.Si
        Str
        i
        ng        ))
        ;
        

        
         ListO
        b
        st        h d        rent prefix         es
     
              
        / prefix val
        e="", expecte
        d
         numbe
         of fi
        l
        s l
        sted=1
        
              var prefix = "        ;
     
                   awa
        t
        Lis
        Obje
        c
        s_Test(mi
        , 
        uckeN        am        epr        e, headers: extractHeader).Config        ur        e
        w
        ait(fa
        l
        e)        ;
        

          
             
          //
        pref
        x
        value=

        ed num        be         of 
        f
        le
         listed=n
        ile
                                          
        refix 
         objec        tN        me
        + "
        ";

                              
             await ListObject        s_
        es
        (min        io        ,
        buck        et        N
        me,
        pre        fi        x,         nFi
        es
         t        ue         h
        e
        ader
        s
        :
         ex
        tra        ct        He        ader
        )

               .C        on        fg        wait        al
        e
        ;

                          
        // pr        fi
         va
        ue="/
        est"
        , expected nu        mb        r of files list
        d
        nFi
        es
               = objectN
        m
         + 
        est";
      
         
             await Lis        O
        (minio, b
        u
        cketNam        e
        ,
         
        p
        refix,         nF        i
        es         t        ue,
        hea
        er        se        ea
                   
        wait(fals
        e
        );

                    /         prefi
        x
         value="/test
        "
        , e        ec        e
         
        n
        m
        er
        s
        refix         =         o
        j
        ctName
        + 
        "
        test
          
        w
        a
        t 
        istObj
        e
        t
        _
        est(min
        i
        pre
        ix, nFil
        s
        , true
         h        a
        e
        s: ext
        r
          
                        C
        nfi
        ureAwa        it        (fa        l
        se);

 
         
        i
        cted         n
        e
        d
         p        re        fi        x =         obj
        e
        ctName + "/t
        e
        st/small
        "
        ;
        it        Lis        t
        bje
        ts_Tes
        (
        ini
        o
        , bucketNam        , prefix,         n        Fi
        l
        e
        s,         t         e        xt        rac
        H
        ader)

         
                                 
         
         
         
        t(
        alse);


         
           
                     // pr        ef        ix 
        v
        alue=
        "
        /t        ed number         o        f
         
        file
        s
         listed=
        n
        iles
    
         
           
          pre        fx        test/sml        l/"
        ;
        

                  ait Lis        Ob
        j
        ects        _Test(m
        i
        n
        i
        e, prefix, nFil        es,         ru         headers:
        ex        tr        ctH        ea        de
        )
                      gr        eA        wai
                                  
        // pre        fi        x         valu        e=        "/        t
        s
        ", ex
        pected numbe of files 
        ist
        d=1
              awa
        t Lis
        Objects        Test(minio, bucketN        me         sin        l
        e
        Objec
        t
        N
        ae, 1, true, head        er        sractHeader)
                              
         
         .Co
        nf        iu        e)        ;
                            new 
        intLogger(        "G        etObj
        ec        i_        1", getO
        jectSi
        natur
        , "T
        sts s3Zip files", Te
        s
        tStatus.PAS
        ,
   
                         DateTi
        e.No
         
         startTime, a
        rgs: args).Lo
        ();
     
         
        }
      
         
         ca
        tch (Exceptio
         ex)
     
         
        {
           
         
        ne
        w
         MintLogger("
        etObjectS3Zip_
        e
        t1", getObjec
        t
        Si
        g
        a
        ure, "
        Tests s3Zip f
        les", Test
        t
        tus.FAIL,
         
         
          
         
         
        DateTi
        m.Now - start
        ime,
        e
        .Me
        sage, ex.T
        o
        String
        (
        , args
        : args).Log(         
         throw;
    
         
          }
      
         
        fi         
        {
          
         
        File.Delet
        (randomFile
        Name);
            Direc        path.
        plit("/")[
        0
        ], tr
        u
        );
       
         
         
           await TearD
        o
        wn(mi
        n
        io        ).Con
        igu
        eAwait
        f
        lse
        );           }

        #region Bucket Notifications

        internal static async Task ListenBucketNotificationsAsync_Test1(MinioClient minio)
        {           
         
        e
        Time.No        ;
                    
        Name = GetRa        do        N
        am        e(        15);
  
         
        =
         GetRandom        ame(
        1
        0)        
              
         
        va         cont        n
        tion
        octet
        -
        strea        m";        var         rg        s =
         
        n
        ew Dict        ionary<s
        t
        ring,
         n        
               {
 
                              
          { "        bu        cketN
        a
        me", b
        u
        ketNam
        e
         {        b
        ectName },
           p
        e,         },
            { "si
           
         
         
        ;
 
                      t        r
        y
        

         
          
        it Setup_T        est(
        i
        i
        o
        , bu
        c
        k
        e
        Name).
        of        ig        e
        wa        it        (f        al
        se        )
        vare        v
        d
        o
        N

);

            var eventsList = new L        is
        entTy
        e>
           
             
         
         
          E
        ven        Ty
        e
        Obj        c
        Cr
        at
        edAll
        }
        
                   var 
        i
        ten        Ar        g
        s
         =         ne         ListenBucketNot        fi        ation
        s
        Args        (
          .
        ithB        uc        et(        bu        cketN
        m)        
                                                      .Wi        th        Ev        e
        n
        t
                           var 
        e
        n
        Bucke        tN        oti
        f
        icationsAsync(
        l
         
         var su
        .Subsc
        r
        ibe(
  
         ev         >
        rece
        Add(        v
        ),
           
         
                    e         { },
        

             
         
         

        { }           
            
         );

           
                
        it Pu        er(m        ni
        o
        , buck        et        Na
        m
        e, objectName,         nu        l
        l
         con        t
        e
        nt        Typ
        e
        ,
      
         
                             

        .GenerateSt        ea
        Fro
        Se        ed        (1
        reAa        it        (f        alse);
        

           
                       /        / wait         fo
        ns
  
                                 var
         
        se;
       
         
        pt         = 0; attem
        p
        t < 10; attem
        p
        t
        {
 
            
         
             
         if         r        >
         0)
    
         
         
                  {
          
         
             
         
                  heck         if th
        re 
        s an
        ne
        pec
         er
        or r
        tu        rn
                       
                     
        e r        ec        eivedJson         
        ist         
        l
        ik
                                
         
         
                             lemented"         i        ro
        . If
        so, we th
        ow an 
        xcepti
         
             /
         and        s
        k
        ip
         
        unning t        hi         test
          
                              
                      
        i
        f(        nt 
         1 && 
        e
        ei
        v
        tart        Wi        h("<        rror>        <C        de>"))
        

                    
                
         
          {
  
         
         
         
            
         
               
         
          // Although
         
        t
        he attribu        e         is 
        c
        all        ed        "
        j
        s

                                          / ret
        ue         i
         list 
        ree        iv        d"          in xm
        l
        
            
            /
        rmat and it        i
         a         e
        onver
         xml
                            
             
                    // int         j
        so         forma
        t
        
                          
         
                                         va        r         ece        ivedJson = X        mt        r
        (received[1].j
        s
        on);        
          

               //        lea        up
         
        the        "Er
        ro        r
         key enc
        psulat
        ng
        "rece
        vedJs        n"                                   //
        d
        ta. This        is        e
        red
         t
        vert 
        son da         
             
         
                                   //         
        ce        ivedJ
        s
        n" i        to
         
        lass
         "        rror        esp
        o
        se"
      
         len
         
        = "{'        Er        r
        or'                                        
         
             va         t        r
        mmedFro
         r        ce        ves        us        (len)
        

                                                                  v
        ed        Fu        l
        trimmed        Fr        nt        Su        str
        i
        ng(0        ,r        Leng        h 
        -
        1);

                   
                                            
         
         var         
        i
        ze        .Des        rialize<
        E
        rror        Re
        s
        p
        u
            
                            Eo        = new 
        n
        xpec        te        dMinio
        x
        epti        on        (e
        r
        r.Mess        ag        e           
                                 if (err        Co        e ==         "        N
        tImplement
        e
        ")
   
         
                                         
         
            
         
              e
        x
        = new N        tI        plem
        e
         
                      
         
         

        x;
                              
                             
         }


                            v
        notif        ic        tion =
         J        so        Seri
        ze<Min
        o
        otificatio
        >
        received[0].jso        n
             
           if (        no        tificatio
        n
        .R        ec        or        d=         
        n
        ll)
  
         
              
         
            
         
          {
   
                                                 a
        l(1, notific

nt);
                                      A        ssert.
        sTrue(
        ot
        ficat
        .Rec        r
        d
        s[        .Con        a
        ns("        3:ObjectCrea
        t
        ed:Pu
        t
        ));
      
         
                                                        As        ser
        t
        I
        s
        rue(
        

               
         
                     
         
        a
        ins        (HttpUtility
        .
        UrlDe
        c
        oe        n.R
        rds[0].s3
        .
        objectMe        ta.key)));
   
                                           As        e
        t.I        Tru
        (cont
        ntTyp
        e.Cont        ai        ns(noti
        f
        icat        o
        jectMet        .
        c
        on
        pe));
   
         
                     
         
            
         
         
                    
         
        e
        d
                      
         
                           
         
         
        }
                             
         
         }                    }

                    
              // sub        sc        rip        ti        n.        isp
        o
        e(        );
  
             
                  if 
        (!eventD        te        ted
        )
        
   
         
        Unexpec        te
        d
        Min
        o
        xcepti        n("
        Fa        le         
        to         etec
        t
        th
        e
         e        xp        ected b        uc        e
         noti        f
        cati
        o
        n
         ev
        e
        n
        t
            n
        e
        r
        tenBuck
        t
        ),
 
         
                       
              liste        nBuc
        k
        et        sSignatur
        e
        ,
    
         
            
                      
        "
        Tes
        t
        s         wh        e
        t
        er         Li        t
        e
        ions 
        asses         fo        r 
        s
        mal        l         ob
        j
        ct",
     
         
         
                                estStat        s.        ASS,         
        DateTime.Now        tar        e, args: args).Log();
                      ca
        ch (No
        Imple
        ente
        Exception ex)
        {
            
        n
        ew MintLogg
        r(nam
        eo        stenBucketNot
        fications
        s
        nc_Test1
        )
        ,
 
                     
         listenBuc
        e
        Notifications
        S
        ig
        n
        ature,
      
                 "
        e
        ts whether Li
        s
        te
        n
        BucketNotific
        tions passe
         
        or small object",
        
                TestS
        atus
        N
        , D
        teTime.Now
         
        - star
        t
        ime, e
        x.Message,
          
         ex.ToString
        (
        , args: ar
        s
        ).         
         }
        c
        a
        ch (Except
        o
        n         {
                    i
        f
        (ex.Message
        =
        =         o
         bucke
        t
        notif
        cation is s
        pecific" +
                      `min
        o` server 
        e
        ndpoi
        n
        s")
      
         
         
            {
        
         
             
         
         /        ect
        d when b
        c
        et 
        otif
        i
        cation
             
         
         
         
        /          ag
        inst AWS.

         
           
            
         
            // Ch
        ec        t                st
        a
        tic bool isAWS(st        t
        )            
        
         
         
           
            var rgx = new Regex("^s3\
        \
        .?        .
        com", Rege
        x
        Options.Ig
        no         
                  
         
         var match
        e
        s         s(e
        dPoint
        ;
             
         
                      return matches.C
        o
        unt > 0;
 
         
                  }


                    
         
        if (En
        v
        ironment.
        Ge        ab
        e(
        AWS_ENDP
        O
        INT
        "
        ) 
        !
        =           
          
         
         
                  n
        t
        Ge
        E
        vi        b
        l(        OINT"
        ))
             
         
          {
 
         
                  
         
             // Th
        i
         is 
        a
        PASS
      
                  M
        i
        tLog
        g
        r(n
        a
        meof(ListenBucketNotif
        i
        c
        t
        on
        s
        A
        s
        ync_Test1),
  
         
             
         
                   listenBucketNotificationsS           
                     
         
          "Te
        st        ste
        B
        uck
        tNotifi
        a
        i
        o
        s passe
         
        or
         
        mall ob
        je
        ct                  tS
        a
        tus.PASS
        ,
         Date
        i
        e
        .N        ar                     }
            }
            else
                  new MintLogger(nameof(ListenBucketNotifica                           listenBucketNotificationsSignature,
             s whether ListenBucketNotifica        ll
        o
        bject",

         
             
         
         
          
              Te
        s
        t
        S
        t
        atus
        .
        FAIL, Date
        T
        ime.Now - start
        T
        im                  (), args: args).Log();
                throw        }
        finally
        {
            await         me).ConfigureAwait(false);
        }
    }

        internal static async Task ListenBucketNotificationsAsync_Test2(MinioClient minio)
              var s
        tTi
        e =         Date        me.
        ow;
                       ar         vents          new Li
        st                               
         
        vent
        yp
        .Oj        ectC        ee            
         };
            

        nioo        tificati        on        a
        w("");
                     ar rxEvents        Li        t 
        =
        ven
        )
        
                
        va
        r bucketN
           
          v        r contet        ype = "appli
        c
        ation/jso
        n
        ";

         
        or        <string         string>
   
         
            {
                     
         
         
         
           { "bucket
        N
        ame", u        ck        t
        N
        a
        ype
         }        ,
 
         
                  {
         "        si        ze", "1        6B        "         ;


         
         
             asyn
         s
        a
        ic 
        k<        tream> ToStre        m(        tring 
        i
        np        ut        )
        

               
         
         
        tr
        a
        m =
         
        new 
        em
        ryS        ream();
                       
        er        (s        r
        am)
        
                  
        r
        e
                  hs        ync().Config
        re        wait(fal;          
         
                                 stream.P        si        ion = 0;
        

        

         
         
            
         
         
          
         
                }

                    
               
        va         bu        ke
        t
        t
        ithBuc
        k
        et(bucke        t
        N
        a
        m
        );
               
         
        va        cketEx
        is        sAsyn
        c(        uck        tExist        A
        rgs).C        on        f
        i
        g
        u
        r
        eAwait(fa
        l
        se);
           
         
               if (!fou        nd        )

         
                              
         
         
        eBucke        tA        gs         =        new
         
                   .W
        i
        thBucket(buc        ke        tName)        ;
           
         
                    
        a
        wa        t m
        i
        n
        i
        o
        .Ma        ke        BucketAsyn
        c
        (ma
        k
        e
        B
        u
        alse)        

         
                       
           }
                       
         
            void
         
        Notif        y(        MinioNt        data)
  
                                       {

         
         
         
        oe        i
        (
                  o

        ot { Records: { } })         re        u
        r
        n
        ;
          f        o
        r
        each (var @eve        n
        ion.Record        s)        
                             
         
                    rxEvn        ts
        ist.Ad        d(        eve        nt        ;
      
             }                                   va        r         ist        en        Ar        g         Li        sn        kN        otificai        nr        s
        (
        ithBucket(        bu        cketName)
                                .
                                         
         var         o        se
        vable         i        .Lis
        Bucke
        Notf        ic        atio
        sAs        yn        (l
        is
           var sub
        s
        crip
        to        n = obse
        r
        vab
        e
        Subscribe
        (
            
         
            
         
         
           
         
        e
        v =>
                {
          
         
                                     r        EventData
        = 
        ev;
       
        ot        if
        (rxEventD        t
        a
        );
            
         
                   },
                                      x => thro        w         n
        n($"OnErro        :         ex.Message}"),        
                                   
         
        w         rg        me
        tExcept
        on(        "S        TOP        PE        D LISTENING FO         BUCK
        T NOT
        TI
        NS\n"
        );

  
          
        o give        enoug         t        m
         f        r the 
        s
        ubs
        b
        r to         be        rea
        d
        
          
         
               
         
        00
        0
        ; // Mill        is        c
        nds

         
                              
         
          a
        w
        a
        i
        ay(sleepTime        ).        on
        i
        gureAwa        t(        f
        ls
        e);

               lJ        so         
        = 
        "
        {\"test\        :         \
        test\"}";
                          us        ng var        stream
        = 
        wa        it ToSt
        ream        igureAwait(fals        )
        ;
                                   var         pu        O
        bj
        t
                .WithObjec        (
        test
        js        n"        
 
                                    .

)
                .        ithC
        nte
        n

                         
          
        .WithStr
        am
        ata(                                
        With
        bject
        S
        ize(st
        eam.Lengt        h
        put
        bje
        t
        rgs
        .Conf
        i
        gureAwait(false);

                    
        // Waits         u        ntil th
        e
         Pu        t event 
        i
        s
         Ti
        es out         i        f
        t
        e e
        v
        ent is 
        n
        ot        caught         n
        ar tim
        out = 3
        0
        00        ;         // 
        i
        l
        i
         
         M
        l
        lis        ec        onds
   
                         var stTime = D        t
        eT        me.UtcNow        ;
                                                   
        hile
        (s
        ntDat
        a
        .json))
                                          
                        a
        w
        ait Task.Delay(wai        T
        i
        f
         ((Dat        eT        e.t        cN        w - 
        nds
        >=         t        meout        )
         
         
                                
         
                throw new         Ar        gu        en        Exc        ep        ti        on        ("Timeout
        :
         
                   }

            foreach 
        (
        rt.A        reE
        ual("s3
        Object        re        ated        Put", ev        ev        nt        Na        e
        ;

  
          
        ew M
        ntL        ogg        er
        (na        ations        sy        nc_
        T
        est2)        ,
                                          
           
        i
        tenBucket
        N
        tifi
        c
        tion
        s
        S
        ign
        a
        t
        ur                  he        etNot        as        ven
         processin
        g
        ",
   
         
                    TestStatus.PASS, DateTim
        e
        .N        : args).Log();
        }
        c
        at              {
            new MintLogger(nameof(ListenBucketNoti        ic        ation
        sA
          l        is        te        nB        uc        ketNo        ti        fica
        t
        onsSi        gn        atu
        r
        e,

         
                                   
         
        "T
        e
        sts whe
        th        ic
        a
        tions pa
        s
        s
        e
         for
         
        onge
        r
         
        eve
        n
        t
         p             
                  tus.FAIL, DateTime.Now - startTime, ex             
                
        e
        x.ToS
        t
        ing(), arg
        s
        :
         args).Log();

         
             
         
             throw;
                         f
        nally

             
         {
 
                  await TearDown(minio, buck
        e
        tName).Conf
        gureA
        wa        alse);
      
         }
    }

        internal static async Task ListenBucketNotificationsAsync_Test3(MinioClient minio)
        {
        var st
        =
         
        var events = new L        i
        s
        t<
        E
        vn        
  
             {
               y        ectr        e
                    
        ;
     
        ata
        = new Minio
        o
        ificati        on        Raw("
        "
        );
                     
        ull;

         
              
         
        ar buc
        k
        t
         
             var         s        uff
        ix = ".js        on        "            r        e
         
        js        n";
  
         
            v
        r args = ne
        w
            {                              
        {
         
        "bucke
        t
        ame", bu
        c
        ketNam         },        
   
         
        ten        Ty        p
         
           
                     { "suf
        f
        i
        x
          
        {
         "si        e"
        ,
                  {           var
        bucket
        E
        xistsArgs 
        =
         
        n
        ew Buck        tExists
        A
        rgs()
        

         
        hBucke
        t
        (bucketN
        m
        )
        ;
        i
        xi
        tsArgs        )g        Aa          {
                       
         
                        v        ar         m        k
        e
        w M
        keBuc
        e
        Args(
        
    
         
                               .Wi        t
        h
        Bucket(bucketN        ame
        )
        ;
        
                                                        aw
        a
        it         mi        ni
        o
        .
        (m
        k
        e
        Bucke
        t
              }

  
         
           
        a
        ti        on        sArgs = 
        n
        ew ListenB
        u
        c
        ()        
   
                                                           
         .WithBucket(bu        ck        e
        tNa        me        )
                            x(su
        f
        f
         
        vent
        

    
         
               var modelJson
        = "{
        \
        t
         var s         Mo        ;

         
                   si
        g
        var wri
        t
        r
        =
        n
        e
         Stre        am        Wr        ir        m);        
             
         
           
         awai.        rit
        e
        Async(m
        o
        (false);           
         
           
         
            aw
        a
        i
        Ay        reA
        ait(false)
        

           
               stream.Position = 0;

                   var putOb
        j
        ectArgs = 
        ne        )
        .Wi
        hObje        t("te
        t
        json"
        )
        
                              .WithB        cket(b
        u
        ck        ete           
          .W        it        hCont        ent
        y
        e(con        te        ntTy        e)                           
        am        )
                   t
        th);

                          
        E
        xcept        o
        n = null;
                    
         
        t
        io        nB
        ck
        tNoti
        it        n
        s
        Async(notifi
        ca
        t
        ionsArg
        s)
        ;
        

        o
        ificati        ns.Subsc        ib
        e
        (
                x => rxEve        t
        ata=        x,
          
         
         
                   
         
        t
        on =
        ex,
  
                       
           )         =        > {}        );        

          
          
                     
        gh         ti        me for the sub        cri
                                                va
        r
         sleepTim
        e
         
        = 10        0;         // Milli
        s
        ec        nds        

         
                  t T
        sk.Delay(l        ep        Ti        m
        e
        ).ConfigureAwait(fal
        se            a
        ait
        minio.P        t
        ectA
        ync(p        utOb
        j
        ectArgs).
        C
        o
        nfigureAwait(f
        a
        lse);
        

                  r s
        Time         = e        .o         
             var w
        ai        Time          25;
         /
                             
        3000;         / Mi        l
        l
        e (strin        .Is        ull
        O
        rEmpty
        (
                                   {
           
         
                      
         
            awa        lay(        aitT        me        .Con        i
        gureA        wa        it(false)
        ;
        
    
         
         

DateTime.UtcNow -         st        ime
        .TotaM        il        lc        ds         = 
        ime
        ut)                   
                               
                              t
        row ne
         Au        "T
        meou        : w
        i
        e         wa        it
        ing fo         e        en        t
                              if (!st
        mpt
        (rxEve
        t
        ata.        json)
        )
        
     
         
                      
         r        tifica        ti        o
        n = 
        J
        so        De        ot        if        ica
        ion>
        (
        rxEve
        n
        tData.        jn        
          
         
          Assert
        .
        IsTru
        (
        rds[        ].
        ventNam
        e
        :Put"
        );

                       ne
        w
         MintLogg        e
        ss                                    
         
        liste        Buc
        k
        etNo        ificationsSig
        n
        atu
        re,
                     
         w
        et        er Listen        B
        ucketN
        o
        tifications passes for no event         pr        oces
        s
        i
                    TestStat        us        PASS,         D        teTim        e.        No        w -         st        ar        tTi
        me        g();
            }
            else if (exception != null)
                  {
                       
        ption;
   
         
            
         
          }
                       
           
         
        lse
     
         
            
         
        
   
         
         
           
         
         
              throw new Argume        ntEx
        e
        ption("Mi
        se
        d Event: Buc        tio
         failed.")
        ;
        
     
         
              }
        }
                cE        ex)
        {
                           new MintL        og        g
        e
        Not        if        ica
        ions        Asyn
        _Test3),
                                li
        ten        Bu        ck        et        tif
        ionsS        i
        ture        ,
                            
          
        stenBucket
        N
        otif
        i
        ations p
        a
        sse
         
        or no eve
        n
         p
        r
        ocessin
        g
        Te        s
        t
        Stat        us        FAI
        L
        ,
         
        ateT        im        e.        ow -
         
        s
        tar
        t
        T
        i
             
                  ex.ToString(        , arg
        g
          thr
        w;
     
         
          }
 
         
                     fi        nally
        

         
                      {
               
         
            a
        w
        a
        n(        o         etName).
        onfigu
        eAwai
        (fal
        e);
            disposable?.Dispose(
        )
        ;
        }
            }

        #endregion

        #region Make Bucket

        internal static async Task MakeBucket_Test1(MinioClient minio)
        {
        
        
        T
        e.No        ;
           
            va         b        cketName = G
        e
        tR        an        d
        o
        w M
        a
        
 
                              i
        me)
        
              
         
        ar beAr
        gs = new Buck
        tExistsArgs
        .Wit
        hB
        tN        m
        );
 
         
                    A        )
         
        thBucke        t(        uck
        etName        );        
     
         
         
        <string
         
        {
                                
        ,         ucketNa        megion", "us                      };

                        try
          
                  {

                   aw        ai        t m
        i
        n
        (mbArgs        ).        C
        onfigure        wa        t(        f      var n         =         wait m
        i
        n
        io.BucketExist
        s
        Async
        (
        be        u
           
                     s        e
        t.        sTrue(        ou        d);

         
        (
        name        of        (MakeB
        u
        cket_Test1)        ,         mk        , "        Te        st
         whet        he        ra        t
         p        ss        es        ",
                     t
        s:         args).Lo
        (;        
           
            }
                     c        a
        t
         
              {
  
         
                         ne
        w         f
        (MakeBucke
        t
        _Te        st        1
        natu
         Ma
        eBucket pa        s
        s",        
          .        AIL
         DateT
        m
        .No
         - startTime
        ,
         
        e
        oStr        ng        ), 
        rgs:         a        gs        .L        g(        );                   t
        h
        row;
 
         
         
        inall
        
     
         
          {
                        
         
                   a        i
        nio.R
        e
        m
        Arg        s)        .o        eAwai
        t
        (
        f
        alse);
               
        }
        
    
        }

        internal static async Task MakeBucket_Test2(MinioClient minio, bool aws = false)
        {
        
         i
        f (        aws)
  
         
                               ret
        ur        n
        t
        Time = Dat
        e
        Ti        me        .Now;
  
         
        e
         = GetRandom        am        (
        1
        0) + ".with
        p
        mb
        Ar        e
        tAr(        Wi
        t
        h
        a)        ;
               
        v
        r be
        A
        sA        rgs(
        

             
         
             .Wi        hBu        ket(bu        ketName)        ;
             
         
          var r        bAr        gs         = new R        e
        mo        ve                         .Wi
        t
        hBucket(b        uc        ke        v
        r 
        rgs = ne         Di
        t
        o
        n
        {
          
                    
         
        u
        ke
        N
        m
        e
         
         "
        s-ea
        t-1
         }

                        tTy        pe        = "Test w        h
        e
         mak
        e bucket pa        ss        es whe
        s a p
        ri        d.
        "
        ;

  
         
             tr        y
         
         
              {
      
         
                     a
        w
        at        uc        etAs        nc(mb
        A
        rgs)        Confi        ure
        A
        wait(f        al        se);
  
         
                                         var found
         
        =         aw        ait
         
        mi        tsA
        yn        (beA
        g
        ).Config
        u
        reAwai
        t
           
                   Assert
        I
        Tr
        ue        fu        tLo
        ger(nam
        o(        Make
        Bucket_Tm        ke        B
        c
        ketSig
        n
        ature,         te        stType,         e        ,
        Time.
        ow -
         
        start
        T
        ime, arg
        s
        :
         args)        .Log();
 
         
             
         }        

         (Except
        i
        on ex)
         
              {         
                   new M
        nt
        ger(        am
        e
        ma        eBu
        ket        gnature, testType
        ,
         Te        tStat
        u
        .F        IL        

                        
           
          Dato        Ti
        e
        ,
         e.        ex.T        S
        tring(        ,         rgs
        :
         arg
        s)        .
        w;

               }
            
                  fi        nally
       
         
        {
         
         
          await minio.        emo
        ve        Bu        cketAs        yn        c(rbA
        r
        gs).
        C
        o
        ;
                        }
           }

        internal static async Task MakeBucket_Test3(MinioClient minio, bool aws = false)
                   return;
  
         
         
        teTime.Now        ;
                var bucketN        am        e =         Ge
        t
         v        r mbA
        gs = new         MakeBu        ck        etArgs()
  
              
          .
        it
        Bucke
        (bucket        Na        m
              .Wit
        h
        Loca
        t
        on("eu-c
        e
        ntr
        l
        1");
             
         
         var
         
        eArg
        s
         
        = n
        e
        w
         B        rg            
        Wi
        h
        Bucket(bu
        ke
        Name
        );         
        etArg
        s
        i
        me)
        g, st
        ing
        
        {
                       
         
           {        "bu        c
        etNam
        e
        , buck
        tName },
                 
         
        { "        r
        ral-1" }
        };
     
         
         try
    
          
         {
                 io.
        akeBucket        A(        bArgs).Co        figureAwa        t(fals
        e
        );         fo        nd = await mino        Bucketx        ists
        A
        t(false);
                                           Asser
        .I        sT        rue
        d)
        
 
                                        ne        w MintLo        gr        (nam        eo        f(Make
        B
        ucke        t_        T
        ), make
        B
        uck
        t
        na        ture        ,         "T        e
        h 
        r
        egion pa
        s
        s
        e
        ",
                   tatu        .
        P
        m
        rtTime,
        .
        }
                             
        c
        atch 
        (
        xception e
        x
        )
                       
              
                    n
        e
        w         ameof(Make
        B
        u
        cket_Te
        st        3)        , makeBucket        tur        Tests whethe        keBucket with region         es",
   
              
             
        estS
        atus.FAIL, DateT
        i
        me.Now - st
        rtTim
        e,        M
        r)        r
        g
        s: 
        a
        
  
                 t
        r
        w;        
                                  
                   
        m
        ove        (r
        b
        eA
        ait(f        al
        e
        ;
 
                     }
    }

        internal static async Task MakeBucket_Test4(MinioClient minio, bool aws = false)
        
                
        i
        f (!a        ws)
  
         
         
        t
        Time =
        

        N
        me = GetRa
        d
        om        .
        ithperio
        d
        ;
        v
        r mbArgs = 
        nw MakeBucket
                  thBuc
        et        (b        ua         
            .W
        i
        t
        hL        ocation("us-w
        e
        st-2"
        )
        ;
        beA
        gs = 
        e
         Buck
        tE        xi        st        sA
        r
        gs()
                            .Wi        th        Bucket
        (
        b
        ucketNa
        gs =         e
        w
         Remov
        e
        Bucke
        t
        A
        hB        ucket(
        b
        ucketN        am        e);
      
         
         
        ar args = new Dicti
        o
        ary<st        ng, str
        ng>
               {
    
                   me", bucke
        t
        Name
         
        ,
 "        o
        n
        , "u
        s
        -
        wes        -
        2
          };
                t
        

        it         m
        nio.MakeBu
        c
        ketAsyn        c
        (mbArgs).C        on        figurea        ;
       var fou        d = awa
        t min        o
        .Bu        beArgs).Co
        n
        figur        wait(f        ls        )
        ;
          
               Ass        e
        Ir        
      
         
         
         
        new
         
        intL
        o
        g
        ger
        (
        n
        am        et        _T        est
        4
        tSign
         
         whet
        er         Mak
        e
        Bu        ck        et        gion
         
        a
        nd bucketname         wi        th         .         pas
        s
        e
        a
        S
                
           Dat
        Time.
        ow -
        startTime, args:
         
        args).Log()
        
    
         
          }

           
         
         catc
        h         etion ex)
           
         
         
          {
        

         Min        tLo
        gger(nameof(M
        keBuck        et_T
        s
        4),         ma        keBu
        c
        ket
        S
                   
                   "T
        es        ts         
        w
        e
        her Mak        ei        th         regi
        n
        and
        bucketname         with
         
        .         t
        Sta        tu        sA        IL,
        

         
                   
        Dat        e.        Time,
         
        t
        ring(), ar
        g
        s: args).L
        o
        g
           
         throw
        

           
                   }
        fina
        l
        ly         
                                  aw
        a
        it minio.R
        e
        moveBucketAs        n
        bAr
        s
        .Co
        fig        re        wa        i
        }
    }

        internal static async Task MakeBucket_Test5(MinioClient minio)
           v
        r startT
        m
         = DateTm        e.        w;
          
             s
        ring b
        ckea         
         
                   var
         a        rg        s
        ona
        s
        {
                     
             
         
        { "bucketName",
         
        buc        ke        tNa
        m
        e
         },
                            
        {
         "reg
        i
        o
         }

             
        ;
        
    
         try

         
               {
                
         
          aw        it        A
        s
        sert.Th        ro        wsExcep        t
        dBucket        am        Exc        ep
        t
        ion>(
        (
        )            
           mini        .Ma
        k
        eBuc        e
        t
        Async        (new MakeBuc
        k
        e
        Args()                                                
         
          .WithB
        u
        ket(bucket
        N
        ame)
        )n        reAw
        ai        (f
        l
        ;
                         
         
         new         M        in        Logg
        e
        r
        (na
        m
        e
        o
        _         a
        keBucke        tS        ig
        at
        u
         
         wh
        th        r M        keB
        u
        cket         r        alidBuc        etNameExcept
        i
        n when bu        ck        tName is         nu        l
        ", T
        e
                
         
          D
        t
        Time.Now        -         st        rt
        T
        ime, ar
        g
        : 
        a
        rgs)        .Log(
        )
        ;
        

                               
            
         
         c        E
        xeption ex)
        {
     
        M
        f(Mak
        Bucke
        t
        _Test        5), makeBucke
        t
        Sign        at        ur
        e
        ,
                                    "Tes        ht        MakeBuck
        t thro
        s Inv
        lidB
        cketNameExceptio
        n
         when bucke
        Name 
        i
         nul
        ", 
        e
        tStat
        us        L,
         
         
         
           
         D        - star
        t
        essage
        ,
         ex
        .ToString(), 
        rgs: args)
        L
        g()        ;            
        hrow        
 
         
           
        }
    }

        internal static async Task MakeBucketLock_Test1(MinioClient minio)
        {
 
         
                      va        r st        rt        i
        me 
        Now
                      
         
        uc        ke        tN        me = GetRandomNa
        m
        e
        ar         bA        rgs = 
        n
        ew         ake        ucket        A
        rgs()
       
        bucket        N
                    
         L        

         
                     v
         
        s
        rgs()                                 
         .Wit        Bucke
        (
        b
         
          r        s =
         
        n
        ew RemoveBuck
        e
           
        Bu        ame);
             
                  var args = new
         
        Dictio
        n
        a
        ry<string, str
        i
        ng>
                    
                     
        { "bu
        k
        tName
        , buc
        k
        etName },
                     r
        e
        gion"
        ,
         "        
              
         };

           
         
           
                aw
        a
        it min
        i
        o.Mak        eB        ucketAsn        (
        m
        Args)        .C        onfi        ue        Await(
        f
        lse         a         fo        und =
        awa        it         m        in        o.B        (beArgs).C
        on        fu        Ai        e);
  
                  st        r
        
                                  new Mi
        tL        og        ger(nameof(
        t1        ),         
        akeBucketS
        i
        gnatur
        e
        , "Te        st        s         whether         Mak
        e
        B
        cket with Lock pass
        e
        ",
                TestStatus.PASS, DateTime.
        N
         args        .Log
        (
        );
 
         
             }        
           
         
           
        c
        tch        (Excep        t
        i
        n 
        e
        x            {
        

         
         
                              
                  n
        Ma        eB        c
        ket_Test1), makeBucketSignature, "Tes
         L        ock passes",
 
         
                                         T
        e
        stSt        tu        .
        Ti        o         artTime,
        ex.Mes
        age, 
        x.To
        tring(), args: a
        r
        gs).Log();

             
         
            
        hro
        ;
             
                   
        ll        y
         
         
           
         
        ov        eBuc
        e
        nf        gu
        e
        wait(f        ls        e);
  
         
          
         
         
        
           }

        #endregion

        #region Put Object

        internal static async Task PutObject_Test1(MinioClient minio)
        {
      
         
         var s        ta        rt
        T
        i
        me         =         DateTime.Now
        
             
         
         b        ketNa        e = GetRa        nd        om
        N
        am         
          var objec        tN        am        e = Getd        ct
        a
        e(1        0)        
                        var c        on        tp        n/octet
        -s        tr        eam";
   
         
         
         n
        w Di
        t
        ona
        y<stri        ng         st
        r
        ing>
 
         
                    {

                   
        "
         bucketName 
        }
        
                           
        ob        b
        ectName         }        

                                           
        {         "o        p
        e", content
        si
        e"         "1
        M
        B" }
                        };
           
             t
        r
        y
        
        {
   
         
             
         
                  Tes
        (mini
        ,
        buck        t
        ame).C        o
        nfigureAwait(fals
        e
        );
   
         
         
                               await Put
        O
        bject
        _T        e
        cketa        me, ob
        j
        ectNa
        m
        e
        Typ
        , nul        l,        
                                              
         
         rsg.GenerateStr        eS        d(1 *
        eAwai        (f
        lse)        ;
              
            
        new Mi
        og        ge
        (nameof(Pu
        Obje
        t
        Test        1)        ,p        ub        j
        e
                        
                  Te
        ts        whether P
        u
        Obje
        c
        asse
        s
         
        for
         
        s
         
        ASS,         D
        t
        eTime.Now
        - 
        s
                  gs: arg        s).L
        o
        g();
 
         
                     }
               c        t
        c
        h
        (Except        io        n ex        )
        ew         Minto        gg        r(        ame
        f(PutObjec
        _Tes
        1), p        tO        ject        ignature,

            
         
              
        "Tests w        hether
         
        PutO
        b
        or small
         
        obj
        c
        ", TestSt
        a
        te        Time
        .
        N
        o
         -         st        a
        r
        Time
        ,
         
        ""        ,
         
            e
        .ToString(), ar        gs        ).Log();
   
        ;
                     
          }
 
         
                              finally
               {
                   
                      await Te
        a
        rDown
        (
        minio, bucke        e.        igureAwa
        t(fals
        );
  
            
        }
    }

        internal static async Task PutObject_Test2(MinioClient minio)
                    var
        st        rtTime =
        D
        teTi
        m
                    ar be         = t        Ne        

        c
        Name = GetRa
        n
        ObjectNam        e(        10        ;
        

        nt        ntTyp        e         =b        t
         
         ar
        c
         stri
        g>        
             
         
          {
                 bucketName", buck        et        Nam
        e
         }        ,
         
          
        ame",
         
        obj        ec        tName }        ,
             
         
           
          { "con        te        nt        ype"
        ,
         
         
           { "        si        e",
         
        "6MB" }        
            
                   
         
        }
        ;
        tr        y
 
         
                            {
        ait
        Setup_Tes        t(        mi        n
        io,         b        uc        ka        e).Con
        f
        i
        ure        Await(false)        
           
        _Te        ter(
        inio,         bu        cket
        ame, o
        jectName, null, contentT        yp        ,
        0, n
        ll,
      
          
                      rsg.        G
        d(         *        MB))
        .
        Con
        i
        ureAwa        it        (fa
        l
        e);        
                                
         
         
        new
         M        in        tgger(nameo
        _Test
        )
        ,         putObjec
        Si
        ga        e
        wt        r
         
        mult        part P        ut        Object
         
        p
        sses",
                                                Te        ateTime.Now - startTime,        a
        gs: a        g
        ).Lo        ();
               }
        c        at        h (E
        ceptio         ex)                      {                                  ne        w 
        M
        intL
        o
        _Test2),
         
        put
        b
        ectSi        ge        , "Test        s         het        h
        er multi
        p
        a
        r
        utO
        b
        ect 
        pa        se        "
        ,
        

         
        Test
        S
        Da        m.        - startT
        me, ""
         ex.M
        ssag
        , ex.ToString(), arg
        s
        ).Log();
  
             
                  r
         }

               f        in
           
            await
        T
        arDown(minio        ,         bc        et
        ame).C
        ni        gu        re
        wait(false);        
  
         
             }
    }

        internal static async Task PutObject_Test3(MinioClient minio)
        
         s        art
        T
        i
        N
        ow;
               v        a
        ame
        = GetR
        n
        Na        me
        15);
               
        Name =
         
        GetRandomO
        b
        j
        ;
 
            
         
        ar 
        tentTyp        e         =         "c
        u
        stom-c
        o
        tent        ty        pe
        "
        v
        D
        ctio        ar        <strin        ,
        stri        ng        >
    
         
                   
        { "        bu        cket
        Na        me        , bucketNam
         
         
         
                               
         { "        co        n
        t
        entType", conte
        nt        Ty        e },
        

         
                   { "
        si        ze        , "
        1
        MB        };
        
    
                  try
 
             
         
        {
            awa
        i
        t Setu
        p
        _
        Te        st(minio, buc
        k
        etNam
        e
        )
        t(fals
        e
        );
          it 
        utO        ject_Te
        s
        ter(mi
        no        ke
        t
        N
        , objectNam        e,         null,         co        n
        entType, 0, null,
                                 
            
        rsg.
        enera        te        tre        B)).Config
        u
        reAw
        a
        t(false)
        ;
                      
         
        n
        ew M        in        t
        L
        f
        es        3),
        p
        utObjec        tS        ga        tu        r
        e,
        
        er         with cu        stom cont
        e
        n
        -type passes", Test
        S
        atusa         sta
        tTime,
        
 
        :         ar        gs).Log(
        )
         }
 
         
           
         
        catch (Ex
        ce        ti
        o
        n ex        
  
         
         
         
        {
              
         
         
         
         new
         
        in        tL        og
        g
        e
        r(n
        a
        m
        e
        ), p        t
        t
           
        with cu        t         passes        ,         estS
        t
        atus.
        F
        A
        tar        e,
                    "", ex.Message, e        String()
         args)
        Log()
        
   
                throw;

         
               }
  
             
        fi        y
           
                                aw        ai
         
        earDown(
        m
        ini
        ConfigureA
        a
        t(false);
                     
         
          
        }
        
    }

        internal static async Task PutObject_Test4(MinioClient minio)
        {

               var startTim        e
         = Dat
        eT
                  v
        r b        ck        tNam         
        =
         GetRa
        n
        omN        am        e(1
        5
        v
        om        jectName(1)        ;
        Name = Cre
        ateFil        (1, dat
        F
        i
        v
        r contentTyp        e
         = "customt                    
         var
         m        et        a
        Data = new Di
        c
        ri        ng        ,string>
  
           { 
        customhead        r
        ", "m        ni           dotn        t" 
        }
        

                        };
    
         
           va        r         a
        r
        ary<s
        ring        ,         string        >
       {
         
         
         {         "b        ucketN        am        eu        keta        e
        },
 
         
        Nam
        e
        ", objectName },
     
         
         
         c        ntType
        "
        ,         ont
        e
        n
           
                   "data        ,
         "1B" 
        }
        ,
                                 { "s
        i
        z
        ", "1B"         
        meta        at
        ", "cus
        om        he        der:m        ii        o-dot        n
         }

         
         tr
        y
                               {

         
                               w        inio
        ,
        buck
        e
        t
        Na        me        )
        .
        C
        o
        t(false);
                                 
          var sta
        Ob
        j=          
          await P
        u
        tObje        t
        _
        Tester(mi        io, b        c
        k
        e
        Name, objectName, 
        f
        e, metaData: m
        Data)        
           
             
          .C        nf        gureAwait(        f;                        A        ss        er        I
        True        (s        atObj        e
        !=
         
                  s
        ert.IsTr
        u
        e
        (
        tatO
        b
        j
        ect
        .
        M
        e
        

         
        ta = new Dictiona
        ing>(
        tat        Ob        ject.M        et        aData
        ,
        StringComp
        a
        r
        er.OrdinalIgn        r
        e
        Case)        

         
        As        .s        (statMet
        .Conta
        nsKey
        "Cus
        omheader"));
  
         
                 As
        ert.I
        sT        bjec        .MetaData
        Co        ta        nsKey        (o        e")
        &&
       
         
                     
         
          
        s
        tj        ec        .Met
        D
        ta["Co        ne        nt-Type"].
        E
        qu
        a
        lc        ustom/con
        e
        ttype"));
   
        w n        (m        (PutO        j
        e
        pu        t
        re,
        
         
              "Tes        ts        w
        h
        ect
         w
         different
        c
        o
        u
        tom header pas        s
        u
        .PASS,
        

             
         
        me        .Now - startT
        as        Log(        ;
        
        }
        
             
         
         catch (Ex
        c
        e
        ptio        n         ex        
       
         
        {
   
         
         
        r(nameo
        f(        utOb
        j
        ct_Test        ), 
        p
        tO        jectSign
        a
        ure,
        

                   
                   
        "
        es        s 
        w
        th 
        d
        in        c
        o
        n
        e
        t-
        t
        yp        e         and         us        om heade
        r
         pa        ss        s
        "
        ,
        L,
                   
         
           DaT        .Now -        st        a
        tTime, ex        Mes        sage, eo        String(
        args: arg
        ).Log();

                      
          t                fi
        n
        ally
        

               {
        

           
         
              awa
        i
         T        ar
        D
        wn(m        n
        i
        o, 
        b
        u
        cketName).ConfigureAwait(
        a
        ls        );
    
          
         
        !I        Fil.        De        lete(        fa        e);
  
         
         
         }
           }

        internal static async Task PutObject_Test5(MinioClient minio)
        {
                    var star
        t
        Tim
         
         DateTi        e.
        N
        w;
          
         
                  var b        c
        et
        N
        ame = Ge
        t
        R
        a
        domN        am        e
        (
        15)
        ;
        

         
        ctNe        adomObjectNam
         args          n        w Dict
        i
        onary
        <
        tring,         tri
        n
        g
        >
                       
   
         
             
         
         
        am        bc        ame },
 
              
           { 
        obje
        tName", objectN
        a
        me },
     
             
         
        B" },
                                        
         { "size", "                      
        ;
                      try
          
         
         
          aw
        i
        etup_Test(m        in        i,        c
        ig
        Await(fa
        s
        );
                      
         
                            await Put        Oj        Ts        er        minio, b
        u
        cketNa
        me         objec
        t
         
         
        sg.Gener        ateS
        treamFromSee
        i
        (fals        e)        ;
     
               ne        w Mint
        o
        g
        j
        ct        _Tes
        t5),         pu        tO
        bjectSignatur
        e
           
        et        t wi        th         
        o con        te        nt        -typ
        e
         pass
        e
         for         s        mall o        bj        et        t
        u
        s.PAS        S,        

                    Dat
        me.Now - sta        rt        Tim
        e
        , a        rgs
        :
        args).        Lo        g(        ;
                }

         
            
         
        catc         (Excep
        t
        o
        n
        ex)

         
          n
        ew        MintLogger(nameof(Put
        O
        b
        e
        t_
        T
        e
        s
        t5), pu        tO        bject        Si        n
        a
        ture,
        
          
        Te        st
         whet        h         ew        th n
        o
         c        nt        ent-type passes         f
        o
        TestS
        atus.FAIL
        
   
              
                     Da        te        i
        me.N
        w - st
        artTime,         "        , ex
        .
        Mess
        a
        e, ex.To
        S
        tri
        g
        ), a        gs).L
        o
        o
        ;
  
         
         
           
         }        

                fin
        {
   
         
                       aw        ai        t
        Te
        a
        ,
        igu
        ait(fal        se        );        

         
                                                }

        internal static async Task PutObject_Test7(MinioClient minio)
        {
     
           var st        ar        tTim
        e         =         De        ;
      
                   G
        e
        Ra
        n
        domName
        (
        5)
        ;
        
       
         
        va        obje
        ct        ame
         
        =
        Name(
        1
         
        Type = 
        t-st
        ;
     
                   v        ar s         new Dictio        ar        <
        s
        tring
        ,
         
                                { "b
        cketNa
        e", b
        cket
        ame },
        
         
           { "objec
        Name"
        ,         cName },
    
               { "        o
        tent        Tyo        Ty        e
        },
                          { "dat        a"         "10KB
        "
         }        ,          { "size", "-1" }
    
         
         
         
                      
         
        ry
                
        {
        

         
                               /        /         u
        object call         i
        h unknown stream siz
        e
        utOb        ectAs        ync 
        a
        l s
        cce        es            
         
          awai
        t
        i
        , bucketName)        .Con
        figur
        ait(fa
        lse        )          
         
         fi
        es        tm        Gen
        e
        ra        eStr
        e
        mFromS
        eed(10 * KB
        {
                    
         
          long size        =
         
                                    var file_w
        s
        ream.Length;
        
          
                                
        t
        bjectA
        rg         = nw         P        ut        

                                           
         
         
                   ithBucke        (buc
        ketN        me)
    
         
             
         
                      
        jec        tN         
         .Wit
        StreamData
        (
        filest        re        m)
                                  
                                                b
        j
        ectSi
        z
        e
           
                                .Wi
        h
        tType
        ;
                                                aw
        a
        it mi
        n
        o.Pu        tO        bj        ctAs
        y
        c(putObjec
        tr        gn        f
        i
        ureAwait
        (
        as        e);

         
        A
        rgs = new Remo
        v
        eObje
        c
        tA              
         
                      
        .
        WithBuckt        (b
                       
         
           .W        it        hO        bj        ct(ob        je        ct        Name        );        
          
        ov
        O
        jec
        Async(rmr        fig
        u
        re        wait(fals
        e
        );
                           }

                                   
         
        new MintLogge        r(        name
        o
        f
        ect_Te
        s
        t7), p
        u
        tObjectS        g
        nature        
                              "Tes
        ts w
        t w        th 
        u
        nk        nown 
        s
        tream-size
         
        passes",         estStatus.P
        A
        SS, DateTime.N
        ow        - 
         a        rgs).Log(
        )
        ;
                      
        }
        
        catch
         
        (E        xc        ept        io        n 
        e
        x)
        {                       
            
        (
        ameof(PutO
        b
        ject_T
        e
        st7), p        ub        ctSignature,
             
         
        ther PutO        bt        h un        nw         stream-si        e         pe         T        stS
        atus.F
        IL, T        .N        w
         - s
        t
          
         
           
         
        "", exe         ex.
        T
        o
        Str
        i
        n
        g
        L
             
        t
        hro        w;        
              
         }
        

        a
         
        a
        wait T        e
        a
        rDown(        mi        io, buck
        e
        ta        me        .ConfigureAw        ai        (fa
        lse);
        }
    }

        internal static async Task PutObject_Test8(MinioClient minio)
        {
                       va        ra        r
        t
        ;
      
         
         va
         
        ucke        Name 
        =
        Ge
        t
        RandomN
        a
        e(
        1
        5);
    
                   var
         
        bjec        N
        a
        me 
        =
         
        G
        tName
        (10);
        var conte        nt        Type "applicat
        ;
                      var         a        gs
         
        = ne        w         D
        it        io        na        ry<strin
        g
        ,
         string>
     
                  {
           
         
         
        "
        , bucketN
        a
        m
        e
        },

         
              
         
            { "o
        b
        j
        ob        Nm        
       
            { 
        conte
        tTyp
        ", contentType 
        }
        ,
         
          { "
        da         "0B" },
    
                               { "
         "-1" }

                             
         
         tr
        
                       {        
  
         
                                       //        Putobjec        t         ca
        ll         
        wn str
        a
         sent 0 bytes.
             Set
        p
        Tes
        (mini        o,         bucke        tN        me).C
        o
        figur        eA        wa        it(false);

        ile
        stream = rsg.Gen
        S
        (0))
             
              {
                       
         
         
         
            
        f
        le_wri
        t
        _siz
         
        m.Length;

  
        put        be        ctArgs = 
        n
        ew Pu
        t
        bjer        s(
             
         
                              
         
        t(buc
        etName)
        
         
             
         
                            .WithOb        je
        c
        (objec        t
          
         
                           .WithStreamr        m)         
             
         
         
        ith
        bject        iz        ez                                   
        .
        W
        thCon        tentType(conte        n
             
            await
        mini
        u
        ObjectA
        s
        ync(        pu        Objec        tA        s).
        onfig
        reAwai
        t(false);
                     
         
            
                  new         em
        o
        veO
        j
        ctArgs()
                   
                                   
         
         
          .
        W
        i
        th        c
             
         
                                .W        it        Ob
        j
        tN        am
        );
       
         
              
         
         awa        it         m        ni        .Re        mo        ve        Ob        je
        c
        t
        sync(rmArgs).C        on        figu
        r
        false        
      
             }

 
            
                            new Min
        t
        Logg
        r(name
        f(        Pc        Test8)         put
        O
        bjec
                       "T
        e
        st         
        u
        Obj        ect whe
        r
         u
        n
        now        n         strea        m         e
        n
        s 
        0
         by        te        s",         es        S
        t
        tus.
        P
        A
        SS        ,e        rt        i
              args: a.        o
                       ca        tE        xi         ex
           
         
           {

         
         
        ew        to        (nameof(
        utObje
        t_Tes
        8), 
        utObjectSignatu
        r
        e,
        
             
                  t
        t w
        ere unkno        w
         
        tream se
        n
        ds 
        0
        est
        tatus.FAIL
         
        at        eT        me.        No        w - star        tT        m
        e,         
           
            e        x.Mess
        ,x        (), 
        ar        g
            
         
         
         
         }          
        ly

            
         
        {
 
                  
        aw        ai        t Tear        D
         b        o
        fig        reAwait        f
        alse)        
               
        

            }

        #endregion

        #region Copy Object

        internal static async Task CopyObject_Test1(MinioClient minio)
        {
                    
                  m
         = Dat
        e
        ime.
        o
        r
         
         G
        m
        ar obj        ctName
        = Ge
        Ran        o
        ObjectN
        me(10)
        
                      var destBuck
        am         
         G        tRando
            v        at        tN        m
         = GetRand
        o
        m
        Name        (10);
           
         
         va        r o
        u
        t
        ame        ;
     
         
        var
         
        args         =         new Dic        ti        nary<st        rin
        g
        , 
        t
        in
         
         "bu
        ketN
        m
           
              o        Na        e", 
        o
        bjectN
        a
        de        tBucket        Nam
        "
         d
        stBucketName 
        }
        ,
        t
        Name", des
        t
        Obje        ctName 
        }
               {
         
         
           }        
        tr        y
                {

         
        _
        Test(mi        ni        o, buc        ke        t
        i
        t(f        le                                est        (m        in
        o,         de        t
        B
        ucketName).C        on        i
        g
        ureAwait(        fals
        e
        )
        ;

                           us        in         (va
        r
         
        m         = r
        g
        Gen
        rateS        tr        eamFro        mS        eed(1
         
        *
                            
        v
        a
        r
        gs()
     
         
                  
         
         
        etN        me        
                
         
              
         
         
          .With        Ob        ject(ob
        j
        ectNa        me        )
        
          
        mDa
        a(filestre
        a
        m)
                                                       .W
        i
        t
        Ob        je        ctSize(file        stream
        .L                       
        /.        r
        (nul)        ;
                                      
         
        awai
        inio.P
        utObjectAsync(pu        tO        bj
        e
        tArgs).C
        o
        nf        ig
        r
        alse
        )
        ;
        
  
                             
                 var co
        o
        urceObjecA        rg        s
         = new Copy
                       
         
         .Wit        hB        ucket(b        uc        ket
        N
        a
        e)
               
         
        ame)
        
        
           v
        r copyO
        jectAr
        g
        s = 
        ew Cop
        yObjectAr        s()

         
                    
         
                
        .
        With        o
        yO        b
        bj
        e
        tA
        r
        gs)
   
         
          
                                         .        Wi        th        Bc        k
        Na        me)

         
         .        W
        me        );


                                  
         
        await
         
        inio.        Co        pyOb
        j
        e
        ctAsync(copyOb
        j
        ect        Arg
        s
        ).ConfigureA        (a        ;

     
              
        ar ge
        Obje
        tArgs = new Get
        O
        bjectArgs()
             
                       .WithBuc
        et(de        su        t
          
            .Withb        je        t(d        es        tObjectN        am        )
        
                    
         
         
        .Wi
        le(outF        il        eNm        e
                       a        w
        ai
        t
         minio.GetObj
        ctAsync(get
        b
        ectArgs).i        it(false);
  
            
         
        Fil
        .D        elete(ou
        t
        FileNa        me        );              
         
        m
        je        c
        Args()
                                        
         
        (
        uck        et        N
                  
         
         
        o
        ctName        ;

                   await m        in        i
        A
        yn        c(rm
        Args
        1)
        .Con
        l
        e;            
         
        r(
        _
        ectSignature, "T        es
        Co
        yObj        ct        asse
        "
         Test
        St             
                               Dat        T
        ime.N
        o
         - startTi
        m
        e
        ,         ar        s: args).Lo
        g
        ();
 
         
                   cat        ch (Ex
        e
        tio
        n
         ex                   {
        

         
         
        M
        yO        jectSig        na        ure,
        s
         
        whethe
        r
        s", TestStatu
        .A        IL        ,

                                      
        D
        a
        M
        e
        ssag        , ex.T
        o
        (
        );
                         
          thro
        n
        ally
        {
        

                  
         
        ame)        
        
         
           a
        w
        ck        et        ame).Configur
        e
        Await(
          awai         Tear        ow        (minio,         de        stB
        u
        cketName).Con
        f
        ig        reAwai        (false)        ;
        

             
         
          }
    }

        internal static async Task CopyObject_Test2(MinioClient minio)
        {                                       var sta
        r
        tTi
         
          var b        uN         = Ge        R
        a
         var 
        bject
        N
        ame = GetRa        nd        om        bjec
        t
        Name(1
        0
        )
        ;
                        destB
        u
        c
        a
           
          va        r         destO        j
        e
        ctNam         
        =
         GetRandomName(
        1
        0
        ;
                                 cti        on        ary
        ri        g, str
        ng>
                                      
   
              
         {         "b        ce        tN        me", 
        bucketNam        e         ,
 
                                        
         
         { "obje
        c
        tNa
        e
        , objece },
        
         
          { 
        "
        d
        esu        Bucke
        N
        ame },
  
          
         
        s
        es
        ObjectName
         
        },
   
                        {        "data"         
        "
        1
         },
                    {         "
        s
           };          
                  ry

              
             
                    /        / e        s         pyCo        ditio
        n
        s wh
        atching
         
        ETa
         
        s not fou        n
        d
        wa
        i
        t Seu        p
        Te
        s
        t(minio,
         
        b
        uk        etN
         
        wa
        t
        ketNa
        e).Cou        it(false);
        

        

                           usi
        n
        g         var 
        f
        i
        ner        treamFromSee        * KB);
            va        tObjectA
        gs = n
        w Put
        bjec
        Args()
         
         
              .With
        ucket
        (b        t
           
                .        t
        Obje        ct        obj
        ec        N
         
                   .        at
        (filestream        

                                     .Wit        Ob
        j
        ec
        t
        rea
        .Length)
           
                i        rs(
        ull);
                 
        await mi        ni        o.        PutO
        b
        je
        c
        t
        jec
        Ar        gs        )f        eAw
        ai
           
         }
 
         
           
        catch (Exc
        e
        ption         ex        )
                     
        {
          aw        it Te        r
        u
        eAwait(false
        )
                                  t        arDown(minio,         es        uc        etName).Confg        ur        et        e)        ;
  
                                  new MintLg        gT        y
         
                  "Tests whethe
        th         Eta
        g
         mism
        a
        ch pass        tu        s
        .
        FAIL,
         
        Da         star
        Time,
    
         
                     
         
           ex        .M        essage, e
        x
        .
        ToStri        ng        (), args
        :
         args        ).        Lo        g)          thr
        w
        ;
          
         
         
                  tr        
               {
            
        v
        a
        o
        py        Cn        ;
          
         
         
        ag");

         
        b
        jectArgs =
         
        new         C        opySour
        c
                     
         
           .WithB        uc        k
        et         
                                       .WithObje
        c
        t(objectNam        e
        )
    
         
         

        yConditions(con        i
        tio
        n
        s
         copO        bjec
        t
        Ar        gs = new Copy
        O
        bjectAr        gs        )
   
         
         
                                          C        pyObj        ec        t
        tAr
        s)                                .        i
        hBu
        ket(
         
              .Wit
        h
        Object(        de        st        b
        j
        ini
        o
        (c        p
        Object        rgs).Co        nf        i
        u
        wa
        t(fa        lse);
     
         
         
        oE        xceptio         ex                                          i        f 
        (
        

                             
         
        r
        ew        it
        h
         
        one o
        e pr
        e
        -condit        io        ns         you sp
        e
        cified         di        d not h        ol        d
        "
        ))
           
         
        {
   
         
         
        nt
        ogg        r(nam        of(Co
        y
        bje
        t_Te        st        2), copyO
        b
        j
         
                          
         
          "Tests w
        j
        ect with 
        E
        tag mismatch p
        a
        s
        .PS        , 
        D
        ateTime.No         
                                    
         
              a        gs: arg
        s
        ).Lo        ()        
      
         
         
            }
        
         
           el
        s
        e
        
            
              
         
                    n        ew MintL
        o
        g
        y_Test2), copy        bjec
        t
        S
                        "Te
        s
        th        Etag m
        i
        sa         Test
        tatu.        L,        Dat
        e
        Tim        e.Now         
        -         s
        tarTime,
              
         
         
        e
        ,ex.ToStrin), args: args).Log();
        

                      th        ow;
   
                       }                       }
  
         
            
         
        x)
              
         
         {        
          
               n        ew         M        i
        tLog
        g
        r(na
        m
        e
        of(        Co        p
        y
        O
        ), copyObjectSi
        n
        atu        e,                                
               "Tes
        yE        ag mismatch pass
        e,        T
        Status.        Fm        o
        w - startTime        

           
         
        ag        eo        g(        ),         args
        :
        ar
        g
        s)        .)                   
         
                     
                {
     
         bu
        c
        k
        re        Aa        it(false
        )
        ;
        
                            awa
        io        o, de
        tBucketN
        a
        me).C
        o
        figure        Aw        ait(fals
        e
        )
        ;
                        }
    
        }

        internal static async Task CopyObject_Test3(MinioClient minio)
        {
 
         
              var s
        artTi
        me        ateTime.Now;

                               va
        ndo
        Name(15);

         
                     vb        ctN
         = Get        Ra        ndo
        O
        jectName(10);
                       
         
        va        r         de        stBucketName 
         GetRa        nd        oN        )
        tOb
        ectName         =         GetRano        mN        me(10);
     
                   v        ar        outFileName =
        "out
        i
        eNa
        e";
               var 
        a
        gs =        ne
        w Dictionar
        g
        
        {

                           {
        "
        b
        e
        Name }        ,
                               
          { "objec
        N
        a
         ,        
                            { "d
        estBucketName        ,         des
        B
        u
                  stO        jectName",
        s
        t
         
              
                   {         "        t
        a"         
                            "
        s
        " }

        
        {
                            
        / e        t CopyC
        iti
        h
        re 
        atchi        na        fou        nd        

                                  a        w
        a
        it Se
        t
        Te        st(mi
        igA        a(        false)        

         
         
                  awai
        t
         Setu
        p
        _(        ucke        tN        a
        e).o        nf        ie        t(false);
          
         
         
        s
        ng
         
        (v        m =
        rsg.Generat        St
        e
        mF        o
        Seed        1 * K        ))
 
         
         
         
                      var p        u
        tObjec
        e
        ctAr        s()
  
         
                           
         
        (
        bucketName)
           
                  
                  b
        jectNam        e)        
                                 
         .With
        S
        r
        eam)
      
                     
         
         
        ectSi
        e(fil
        e
        strea        m.        Len        gt        h
                        
         
         
            await mi        ni
        t
        ectAr
        s
        ).Co        nf        igure
        wa
        i
        
           }


                     tO        jectAr
        g
        s = ne        w         gs()

                
         
             
         
        WithBucket(buc
        k
        e
        tName)
      
         
             
         
         
        obj
        ctName);

         
                   var sta
        t
         = a
        Async(statObjA        gu
        eAwa        it        false        );        
 
              
            var condit        io        ns =
         
         CopyCon
        d
        iti
        n
        ();
             
         
        tc
        h
        ETag(st
        a
        s.        ET        ag        );
     
         
         
         
           va        r         opyS
        o
        u
        rce
        O
        b
        je         Cop        So        u
                   .W
        et        Nm        e                     
         
        .
        W
          
           
        op        yConditi
        n
        ond
        tions);
          
        O
        bjectArgs         =
         
        new CopyOb
        j
         
               .W        it        h
        C
        opyObjectSo        u
        ceObjectArg
        s
        )
                                             .WithBuck        et        (des
        B
        cke
        Name)        
                  
         
         
        O
        bjectName);

          t mi        i
        o.        c
        opy        bjec        Arg
        s
        )
        ;
                          
         
         var ge        tA        ew G        eb        A
        rgs(        
         
         
         
             .        it        Buc        e
        t
        (dest
        B
        u
                  
        Wi
        thObject(des        
           
                  await mi
        io.G        e
        Asy
        cg        t
        bje        ct        A
        rgs).Confi
        ure
        wai        t(        false)
        
  
           
                     st        a
        t
        w
        gs(        
    
         
              
         
                   .W        thBu        ket(des
        t
        B
        cketNa        me        )
        stObject)                     
           vr         d        sta
        s = awai
         m        it        je        c
        tAsy
        n(        st        atObjec
        t
        Arg
        )
        Configure
        A
          As
        s
        rt.I        sN        o
        t
        Nul
        l
        (
        d
         
        e(d        st        a
        tOb
        ectNam        e)        )                new Mi
        n
        t
        ogger("Cop        yO        be        ectS        iu        Te
        ts w
        et        he        r Copy
        bject 
        with Eta         matc
        h
         pas
        s
        s",
                      
         
                     
                  TestStat
        u
        ta        r
        Ti
        m
        e, args:
         
        a
        r
        s).L
        o
        ();

         
         
           
         
         
                  cepti
        o
        {
         
        b
        j
         
        atu
        e,         Tests         w        e
        t
        her         C        op        yO
        b
        ject         w        i         a
        t
        h passes",
                         
         
        IL, i         - star
        Time
         ex.
        essage, 
        x.ToS        tr
        ing(,         args:
         
        args
        )
        Log();
 
         
                    
         
           throw;
        

           
         
          
         
        finally

         
         
         
            {        
                               
         
         
          F
        i
        l
        e
        );
            await TearDo
        nfi        gu        rw        (false)
        ;
        
                                 Down        (m
        nio, des
        t
        Bucke
        t
        ame        .Configur        eA        wa
        i
        t
        (f        al        e);
       
         
        }
            }

        internal static async Task CopyObject_Test4(MinioClient minio)
        {
      
         var 
        st        i
        me.
        ow        
               
        v
        r bucket
        N
        ame
          GetRandomN;                      va
        r
         o
        b
        j
        etR
        ndomObje        N
        me(10);
                      var d
        e
        st
        B
        u
        Get
        andomName(15);
             var dest
        O
        bj
        em         Get        andomNa
        e
        10);
       
         
        va
        r
         
        e =
        "outFileNam
        ";                               ar a        g
        s = new Dictioa        ry        st
        i
        g,         s
        ring>
            {
         {        e"         b        ut        me        },
 
         
         
        e
        tName",        ob        jt         }         
        { "        destBucketNam
        e", des        Bu        cm         
           { "data", "1K
        B         },        

        K
        " }
          
            
         
         }
        ;
  
         
                  {            
        e
        l
        ed t
         soure         obe        tN        me
               
          aw
        it
        SetupT        uck        et        Na
        e).        Cn        figu
        r
        eA               awai
        t
         Setu
        p
        _
        Bucke        tN        me).C        on        figur
        e
        Await
        (
        alse);

              
         
         
           using (var
         
        files
        t
        r
        eS        re
        r
        omS
        ed(1 * KB)
        
            
         
              {
                                     v
        a
        r
        p
        tO
        b
        j
        rgs
        )
                         .        WithBu
        c
        k
                  .WithObjec
        t
         
                 .
        W
        ith        StreamDa
        t
         
                               je        ctS
        i
        ;
             
         
                                 awa
        i
        t mini
        o
        .
        jectA
        gs).i        uw        it(fal        e)        ;
        

                           }
        
          n        w C
        pyConditions(        );        

         
           
             condition
        s
        .
        );
       
         
                    //         o        mit d
        e
         cop        Source
        O
        b
        Cop
        Sour        ce        j
        c)             
         
         .WithBucket(b
        u
        c
        ketName)              
         
             
         
         
        jec
        Name);
   
         
          var co        py        Obje        tA        g
        s
         
        gs(        )
            
         
                              
         
           .
        W
        i
        e(c
        pySour        ce        Ob        ject        Ar        gs        )
      
         
           
           .Wite        

                        
         
        bj        e
        alse);

                                 var g
        e
        t
        ew         GetObjec
        A
        ()

                        
        b
        ucket        Na        me)
                   .WithObjec        t(        ob         
                 .
        W
        ithF        le        outF        leNa
        m
        tO        bjectAsyn
        c
        (e        .Conf
        gureA        w
        a
        it(false);
    
         
                       var stat        Oe        Ob        j
           
               .WithB
        c
        et(
        estBucketNa        me)
        

         
        O
        bject(obje        ct        N
        ame);        
          
         aw        ait mini
        o
        .StatObjectAsy
        n
        ureAw
        it(        al
        s
        e)                      
         
         Ass        rt.IsNo        tNu
        l
        l
        (stats        ;
              
         
            A
        s
        .Obj        eN        at        in
        s
        (
         
            new Mi
        n
        tLog        ge        r(        CopyObje
        cs        ", copyObjec
        t
        S
          
                      
         
        Tests        hethe
        r
         CopyObjec        t def        a
        u
        lts target        Na        me t         o        jectName",         stStatus.PAS        S,        Da
        t
        eTime        .N        ow -         s        ta        r
         
         
            a        .Log        );
    
         
         
                  tch
        (Ex        ce        pt        on ex
        )
        
        {
       
         
           new MintLogger("
        C
        pyOb        ec
        _T        st4",
        copyO        jectSg        natu
        e,
 
                                                     T
        es
        ct defaul        tm         obj        ctN        am        e
        "
         Tes
        t
        tu        s.        A
        I
        L
        ,         Da        e
        T
        i
        ime,
          
         
            ex.M        es        o
        );        

        ow;
         
         
            }
                        fi
        ally
                       
        {
  
            
                    F        l
        .Delet
        e(
          
         
        await Te
        a
        rDo
        n
        minio         buck        Na
        me        .Confi
        g
        A
        w
        ait(fa        lse
        )
        ;
        

            
         
           aw        ai         Te
        a
        r
        D
        cketN
        a
        alse);

         }

        internal static async Task CopyObject_Test5(MinioClient minio)
            var start
        T
        ime =
         D        ae             
        var buck
        e
        tN        me 
        =
        GetR        an        domNam        e(        5);
        

         
               var ob        je        ct        N
        ame =
         
        G
        ec        e1                
        ar des
        Bucke
        Name
        = GetRandomName(
        1
        5);
       
        var d
        es        e
        etRn        domName0          
                  //
         
        Fi        e
        ame = "        ou        tFil        N
        me";
        
        v
        ar
         
        args = new Di        ct        on        ar        yt        stri        g>
        {          
         
          
         
              { "buck
        tN        am        e",         b        ue        tNam         },
     
         
          
         
                  ect
        ame",         o        bje        ctName 
        ,
                             "        es
        t
        B
         de        s
        N
        me        },
                   { "destObjec
        Name        "
         
        est
        bjectName
         
        },
                   
         
                    { 
        "data", "6M"         
            { "size"
        ,
        "6MB" }
  
         
         
                  ry
  
              {
        
         
        ulti-part copy         u
        pload for large file
        e
        ted.
 
         
                     
            t        tu        _Te
        ck        e
        t
        wi         S        tu
        _T        t(mini
        , destBuck
        gur        Awa        t(false);

         
             
         
            using        (
        v
        a
        r filest        re        am = r
        s
        g.Gen
        e
        ra        Seed(        6          MB))        
                             
         
         
          var put        Ob        jectAr        gs        = ne
        w
         
             
         
           
                      .W        iu        ket(bc        ke        tNam        )

         
         
         
          
         
        Ob        
                                                             W        amDa        a(         
                                  j        e
        stream.Len        g
         a        wa        it minio.Put        O
        j
        ectArgs).Con        fA        a
                    }
        

                                v        onditi        ns
         
        =
         new CopyCondi
        t
        ions(
        )
        ;         ni        eRa
        ge(1024, 6
        9
        455
        ;
                    
        /
        /
         
         name.
                               a
        r

        ctArgs = n        eS        Args()
                                Buck        t(bucketNa        e)
   
         
         
        e
        ct(objectN
        a
        me        )
                         
         
        ondi        ions
        )
        ;
        var
        copyObjectArgs         
        ec
        t
        A
         .        Wi        ty        c
        t
        So        rce(copySourceObje
        c
         
          .WithBuc
        k
        et(destBucketN
        a
        m
        awao        Obje
        c
        tr        eAw
        it        (f        a           
           var st        tb        j
        e
        ct        Args()
  
                  h
        Bucket(des
        t
        BucketName
        )
        Wi        th        Object(
        o
        bjectName)        ;         tats = await         m        i
        nio.StatObjec
        t
        A
        sync(        tatObje        ct        Ar
        g
        s).Co
        n
        f
        e);
                    As
        e
        t.I
        NotNull
         
            As        er        .I
        sT        ue(stats.Obje
        c
        s(        objec
        t
        Name));
           
         
         
        reEqu
        l(629        14        5
        5 - 1024 + 1,         s
           new        Mi
        n
        tLog        ge        r
        (
        "
        5", co
        p
        yObjectS        ig        na        t
        ure,

         
         
        Te        sw         
        CopyObject
         
         multi-p        ar        t
        r l
        rge         f        iles wo
        r
        ks", TestS        tatus.PA
        SS,
                            
        rtTim
        , args:         a        gs).Log();
                                }

               cat
        h 
        NotImpleme
        ntedException ex)
                        {
  
         
           
         
           n
        bjec
        t
        Test
        5
        "
        , c
        o
        p
        y
        ure,
                    
           "Tests
        whe        ter CopyObj
         co
        y upload f
        o
        r         la        ge files        works"
        , T        stStatus.NA,
             
         
        Now         -a        im
        , ex.Mes
        age, ex.To
        tr
        ng(), args:         a        s).L        o(        );

         
                                       }
                                (        xce
        p
          
         
               
         
        ne
        w
         Mg        j
        est5", copyO
        b
        re,
                     
        h
        t mu        t
        i-part         
        c
        rge f
        les         w        orks        ,         estSt
        a
        us.FAIL,
          
         
         
                                           D        ate
        T
        ime.No        w         -
        Messa
        e,         ex        .T        on         args:         ;        
   
         
                  ;            }
   
            fi
        ally

            
          {
            
        /
        / File.Dele
        e(out
        Fi        me);
        
           await 
        e
        rDown(mi
        n
        io,
         bucketName).
        onfigureAw
        i
        (false);
    
         
          
         
            await Tea
        Down(minio
         
        estBucketName).Conf
        i
        gu
        r
        eAwait(false)
        
        }
   
        }

        internal static async Task CopyObject_Test6(MinioClient minio)
        
          
        r sta        tTi
        e
        = DateTime        No
        w;
        ar         b
        etNa
        e
        = G
        tRandomNam
        e
        (15);

         
              
        var objectN
        j
        ctName        10);

                var dest
        a
        domName(        5);
        
        var de        tO        e
        Rando        mN        a(        0);                   va
         
        ou        "
        utFileName"        
   
         
        ar a        gs = new 
        t
        i
        ti        ng>
  
         
            {
         
         
        u
        ketName        ",        bucke
        N
         { 
        e
        ,
  
         
            
            
         "        destB
        cke
        Name"
         desB        tN        me 
        }
        "dest
        bjectNam         este        a            {
        "data        , "1K
        B
        " },
         
                {        ize", "1K
        B
        " }
 
         
         
        ry
  
                             {        

                  
        /
         T        es        t
         
        Copy        Co        nditions where mat
        c
        h
        n
         E
        T
        ag         
        _Te
        t(minio, buck
        t
        ame        )
         
             await
         
        Setup_Test
        (
        onfigu
        r
        eAwait(fal
        se         
        (var filestrea
        m
         = rsg.Gen
        e
         
        KB))
         
         
          {
               
         
                     ectAr
        s = n
        e
        w Pub                         
         
         
                 .WiB        a)           
             .With
        ec        (ob
        ectName)
                       
                                      StreamData
        (
        filestream        
 
         
            
         
                                         
        (f        i
        th);
        
                    ai        nio.Pu
        O
        jec
        Async(putObjectArgs)
        .
        Co        s
        e);
      
         
             }

  
         
         s        ta
        t
        ObjectAr        gs         =
         
                                    .B        et(b        cketName)
    
        Wit        hO        b(        c
        tName);
                            va
        r
         stats = await         m
        e
        ctAsync(        st        at
        O
        bjectArgs).C        on        fw        ait(f
        lse);
        

        
            va
        r
         conditio        ns         = ne
        w
         
        CopyConditio        s(
        )
        ;
   
         
         
        ons
        SetModif        ie        ime(20
        1
        7
         
            /        / Shou
        l
        d co        py         object sin        c         ion date h
        ea        de        r < objec
        t
         m        dat
        .
   
         
             
        var         co
        p
        ySourceObj        ec        tA        rgs 
        =
         new CopySour        ce        O
        b
        j
        ect        Ar        g)         
         
                  cke        (b        c
        k
        et        ame)
  
        hObjec
        t
        (o        bject
        N
        ame)

         
                               t
        h
        CopyCo        nd        itio
        n
        s
        (
            va         
        c
        opyObje        c
        A
        gs =
        n
        w
         
        opyOb
        j
        ectA
        r
        gs           
                   .W        ithCopyO        bj        eS        u
        rce(co        py        SourceObjectAr        gs)
        

            .
        ithBuckt        (destBucke
        e)
  
         
                     W
        th        bje
        t(des
        Obj        ec        tN
        ame);

                    
        a
        ctAsyn        (
        opyO
        b
        ectA
        r
        g
        s).
        C
        o
        nf        tfalse);
              
          var getOb        jectArgs = ne
         G
        etObjectArg
         u        estBu        ck        tName        )
                   
                                                Wit
        h
        ame)
                 
             .Wh        out        i
        eName)
        
                    
             
         awai
         mini
        o.        G
        tObjectAsy
        n
        c(
        g
        it(
        a
        se);
             
          
         
          st        tOb
        j
        ct
        A
        rgs        = n        w 
        S
        t
        a
        Obje
        c
        Args
        (
        )
        
  
         
         
         
        .W        (dest
        u
        cketN        a
        thO        bj        ct(destObj
        e
        ctName);
                 
           var ds        ts        ai
        t
        sc        O
        jectA        r
        g
        s).Cn        fA        ait
        fa        se)
        
    
                As        se        t.
        I
        sNot
        N
                     As        ser
        t
        .Is
        rt        .Objec        N
        a
        e.
        C
        ontains(
        d
        e
        s
        Obje
        c
        Name
        )
        )
        ;
 
         
         
         
        tLogg
        er("CopyObj
        , c        pyOb
        r
                   
         "Te
        s
        ts wh        t
        h
        er C        opyObjec
        t
         
        test 
         m        difie
        d
         da        te         p
        as        es", TestS
        t
        at        s.PASS,
                      
             
         
         
        tartT
        me,         rgs:
         
        args)
        );
        }

         
         
                      c        atc        h         (Excep
        t
        ion e
        x
        

                  ew MintL
        gger("
        opyOb
        ect_
        est6", copyObjec
        t
        Signature,

             
                      "Tests wh
        ther Copy
        b
        ect with
         
        pos
        itive test fo
         modified 
        a
        e passes", Te
        s
        tS
        t
        atus.FAIL,
  
                  
         
        DateTime.Now - star
        t
        Ti
        m
        e, ex.Message
         ex.ToString()
         
        rgs: args).Lo
        g
        ()
        ;
        
            
        hrow;
        
        

               finall
        y
        
 
         
              {
     
              File.
        e
        ete(outFileNa
        me);
        
           a
        a
        t T
        arDown(min
        i
        o, buc
        k
        tName)
        .ConfigureAw         
                  aw
        a
        t TearDown
        m
        in        e
        Name).Config
        u
        eAwait(fal
        e
        );         
         }

        internal static async Task CopyObject_Test7(MinioClient minio)
        {
                                va
        D
        teTime
        .
        ow;
                     
        e 
        m
                  ar o
        ctName = GetRa
        domOb
        ectN        am        e(        0)                              
         v
        r des
        e =         e
        t
        Rando
        m
        ame(1        );           
         
        = Get
        domName(10)        ;v        s = new         D        iction
        a
        ry<s        tr
        i
        ng             {        

           
                {         "b        c
        etN
        a
        me",         
          
         
         
        t
        ,
 
                  {         d
        s
        Bu        ck        e
        Name", destBu
        c
        k
        "
        destObject
        N
        ame", dest
        O
         
          { d        ata
        "
        , "        KB" },

         
        1
        KB" }
        }        ;
                tr
        y
        s
        t C        ns
         
        we         is f
        und
 
         
                                a        ait S
        e
        tup_Test(mini
        o,         b        ucketName).Con
        f
        igure        A
        w
        a
         
        etup        _T        est(minio,
        d
        (
        false);
  
         
                 u
        s
        am = rsg.G
        e
        nerate        Strea
        m
        Fr        B))        
              
         
          {
 
                                var
         
        putOb        je        ctArgs = 
        n
        e
        w PutObjectArg
        s
        ()           
         
                  Wit        hB        cket(bucke
        N
        me)        

                      
                   .        W
        ctNa        e)
        
         
           
               .
        W
        ith        St        r

                                   
            
         .With
        bject
        ize(filestre
        m.Le
        gt        h);
 
         
                                   await min
        o.Pu        O
        jec
        Arg        s)        .ConfigureAwait(fl        se        ;
 
                  }
        
                          
         
         
        =
         new StatO
        b
        ject        Ar        gs()
 
         
          .With        Bu        c
        ket(buc        ke        tNa
        m
        .
        With        Ob        ject(objectNa        me        );        
a        s =a        wa        it minio.        ta        Ob
        ec        As        n
        atObje        ct        Args)        .C        on
        f
        i
        

                    var co        ndi
        t
        ions = new CopyCondi
        t
                   var mod
        i
        fiedDate          a
        t
         
                  modifie        D
        a
        te =        modified        Dat
        e
        .
                   condit
        io        s.Set        Mo        ified        (m        od        i
        f
        ie        dD        at        e);
        
         
           //
         
        S
        py 
        bject since m
        d
        fic        io        n date head         m
        odificatio
        n
         date.
       
         
         
         {
                      
         
        e
        ObjectAr
        g
        s = ne         Copy
        S
        
                          
         
                      .WithBu        ck        e
        t(        be          
                              
         
         
        ctName)        
                                 
         
                              .W        it        CopyC        on        ition        s(        c
         
               var
         
        copyO        je        ctArgs         opyObjec
        t
        Args()
                         
         
         
        Cop
        Object
        o
        rce(c        oS        urce
        O
        bjectA        gs)
                      
         
         
                    .W        thBuc
        k
        e)
   
                                                   stObje        ct        N
        ame        ;
 
         
              
         
               a        wa        t
         m        in        io.Copy
        Ob        je        ct        As        nc(co        py        bject        Ar        gs        )
        se);
           
         
                
         
             cc        E
         ex)        
           
                                {
                           
        ert.
        qual(
                        
                       
           "MinIO         A        PI r
        e
        spon
        d
         of th         p
        r
        -con
        d
        ons 
        yu        s
        pecified di
        
    
         
                                   
                  e
           
        }

               
         
                  new Mi        tLogg        r("C
        opyObject_Tes        t7        ", copyOb
        j
                          "Tes
        s whe        he         Co
        yObj
         wit        h neg
        tive
        tes        for modi
        d         a
        e pass
        es", T        es        tSt        at        us.P
        A
        SS,

         
        ime.Now 
        -
         st
        r
        Time,         a        rgs        :         a
        r
        s)
        .
        Log();
           
         
          }
    
         
         
        ch 
        (
        xcep
        t
        i
        on         ex        )
        
          
            
        n
        ject_Te        s
        e
            
                               
         
         "Tests wh        et
        h
        e
        it         n        g
        ti        e test
         
        for         mo        d
        i
        ied         da        e pas
        se        s
        ", TestStatus.
        F
        AIL,

         
         
        Dat        eT        m
        .Now - st        rt        Time
        ,
        ex.Message         ex.
        T
        o
        String(        ),        args: 
        a
        rgs)        Lo        g
        ();
       
        ho               }
              
         fina
        ly
 
              {
        
         
           await Te
        rDown
        (m        , bucketName)
        Configure
        w
        it(false
        )
        ;
 
                   aw
        it TearDow
        (
        inio, destBuc
        k
        et
        N
        ame).Configur
        Await(fals
        )
        
        }
    }

        internal static async Task CopyObject_Test8(MinioClient minio)
        
               
         
        va
        r
         startTime = 
        ateT
        m
        .No
        ;
                        va
        r
         buc        ke        tN
        a
        e = Ge
        tR        m
        v
        r obj        ctNa        me         =
         Ge
        a(10);
    
        c
        etN        me        = GetR        ndom
        Name(15);
                            Ob        jectName =         Ge        tRa
        ndomName        (
         
        ar arg        s         =
        new D
        c
        t
        in        g>
        
        {
  
                  { "        bm        e"        ,
        bucketName },

             
           { "ob
        ectN
        me
        ,         bje
             
         {         "        destBuck
        e
        tName        "
        ,
        destBucket
        N
        a
        me }        ,
                                  
        , destObj
        e
        ctNam
        e         ,          }        ,
   
         
         
        ,         "           
         
          {
         
        "c        op        yconditions"        ,         "x-amz-m
        e
        t
        d
        ta
        -
        d
        E
           
        ry
        {

                                       
          await Setup        _T        e
        s
        n
        figureAwai
        t
        (false        ;
          
         
        e
        sn        destBu
        c
        fa        ls        e);
  
        r
        eam =        rsg.Ge        era
        t
        eStreamFr        m
        S
        eed        1 *
         
        K
             
                     
         
          vau        Obje        tArg
        s
         
        = new PutObjec
        t
        Args
        (
        )
         
        e)           
                                        
        .
        W
        N
        ame)
              
         
                                   (f        ilestream)
        

                  
         
                  Obj
        ctSiz
        (
        files
        ream.
        L
        ength)
                                 .Wi
        t
        h
        Head        er        s
        ng,
        string        >         { { 
        O
        ig"
         "orig-val         with
         
         
        sp           
                                    aw
        i
         mi        n
        ObjectArgs        ).        Co
        f
        gureAwait(fa        ls        e
           }

             
         
              var s
        t
        atObj        ec        tArgs =
         
        n

        gs
                    
             
        WithB
        ck        t(        ucketNam
        )
  
              
         
            .W
        th        Object(obje        ct        ame)
        ;
        i
        nc        (statObje        ct        A
        g
        ).C
        nfigu        r
        s
        sert.IsTru        e
        (
        stats.Meta
        D
             v
        a
        o
        nditions();
      
         
         
        eta
        aDirective();

         
           
              // s        et         cus
        t
        o
                    ar cust        mM        tad
        a
        ta = new Di        ct        ionary<st
        r
         
        {
                      { "
        C
        t
        ion/css" }
        ,
        
                      
         
         
         test"        },
  
         
                             { "
        O
        rig", "orig-va
        ltspaces" }

         
         
         
        opySo
        r
        cb        tA
        r
        o
           
                      
         
        )
                                .W        i(        tN
        me)
                             
                                 W        ith
        op        yConditi
        ns
        j
        y
           
                 .
        W
        ith        CopyObjectSourc
        e(copySour        cb                     W
        thBucke
        (de        tBuce        Name
        
                            
         .W
        thObj        c
        (de        t
        bject        Na
        me)
                                
        .
        ers
        (
        cuso        mM        tad        ta        ;

 
         
            
         
                   a        a
        it         m        ini
        o
        .
        C
        y
        tAg        s)        Confi        gu        re        Aw
        it
        (
             statObjectArg         a
           .WithBuck        et        (e        cke
        Name        
   
            
           
         .Wi        th        bje
        t(de
        tObj        ectN        ame);
                            va
        r
        nio.Stat        b
        jec
        A
        ync(statOb        je        tA
        r
        gs).Con
        f
        gu
        re        Aw        ai        t(fals
        e
        )
        ;
            
                               
        A
        s
        sert        .
        I
        s
        Meta
        Data["Conte
        = nul        );
         
        (dsta
        s.M        taData        [
        "Myne
        w
        ey"] != nu
        ll        ;
                          As
        s
        ert.Is        Tr        u
        ata        "C
        ntent-Ty        e"        .Con
        t
        ins("app        li        cat        io        n/c
        s
        s
        "));
                 
         
         Asse        t
        at        tD        "Mynewke
        "].Con
        ains(
        test
          test"));
     
         
              new M
        ntLog
        ge        opyObject_Tes
        8", copyO
        j
        ctSignat
        u
        re,
        
            
           "Tests 
        h
        ther CopyObje
        c
        t 
        w
        ith metadata 
        eplacement
        p
        sses", TestStatus.P
        A
        SS
        ,
         DateTime.Now
        - startTime,
 
         
                    a
        r
        gs
        :
         args).Log();
                }
    
         
         catch (Excep
        t
        io
        n
         ex)
        
        
   
         
           
          new Mint
        L
        ogger(
        "
        opyObj
        ect_Test8",         g
        ature,
     
         
                 "
        e
        st        p
        Object with 
        m
        tadata rep
        a
        ce        ,
        TestStatus.FAIL,
         
        ateTime.Now - 
        t
        ar         
                 ex.Mess
        a
        e, ex.ToString
        )
        ,         L
        g();
 
         
             
         
                   
         }
   
         
           fi
        a
        ll         
                  await 
        T
        arDown(minio, bucketName).Configur
        Await(false
        );
            await Tea         dest
        ucketName)
        .
        Confi
        g
        reAwait(fa
        l
        s
        e);
        }

         
           }

        internal static async Task CopyObject_Test9(MinioClient minio)
                             v
        r
        sta
        r
        tTime =         D        ateTime.Now;        
            
         
         
         
         
           
          var objectN        a
        =
        Get
        andomObje        ct        Name(        10        )
        =
         
                   s        tN        ame
         
         
             var outFi
        l
        eName = "o
        u
        r
        gs = new        Dictio        ar        y<stri        g, s
        tr        in        g>
  
                  "
        buc        ke        tN        ame", be          
                      { 
        "
        b
        e
        am        ",
         o        ectName },
   
            { 
        "destBu
        ket        am        },
              { "de
        s
        t
        Obj        ct        ame", des
        t
        Objec
        t
        Na         
           
                   {        
           
        a
        te        _T
        me).Co        figu
        r
        eAwait(fal        e
         
        Se        up_Test(
        m
        inio, dest
        Bu        ca        eo        eA
        w
        ait(false);
                   
         
                      usa        tr        eam 
        =
         rsg.Ge        ne        rateS
        t
        reamF        r1        
                   {
                
         
         
         
        w Pt        ObjectArgs
        (
        )
        


           .WithBuc        ke        tk        
  
                                        
        .
        ith
        bject(obje
        c
        tNa        m
                  t
        (f        ie                     
         
           
          .W        i
        
   
            
        o
        Pu        tO        bje
        ctAs
        ync(
        p
        utO        bjectArgs).Con
        fig
                       v        ar putTags                  o
                   
                  
         
         { "key1        , 
        "
                             };
                    s        t
        Se        tO        bjectTagsArgs()
            
         
                       .With
        Bu        

                                   
               .WithOb
         
                                   
           .W        thTaggin        g(
        T
        gT        gs))        ;
                     
         
         
        ject        Ta
        sAsyn        c(        se        tj        tA        gs).
        C
        o
        nfigureAwait        fal        s           }

         
         
        var
        cops        e
        w Dictiona
        r
        y<string,         trin
        g
         
                          
         
         {         key1", "Cop
        y
        Ob                                
         
           var         copy        ou        ceO
        b
        jectArgs = n        ew         C
        o
        p
        ySou        rc        Obj        ectArgs
        (
        )
                                
        ck        et        (buc
        ke        tN        a)                         
         
             .i        O
        ject
        (
        o
         
                      
        /
        / CopyOb
        je        ct tes        t         o 
        replace o        ri
        g
        in                                
        v
        ar coy        = n
        e
        w C        pyObj
        e
        ctAr
        gs()
                       
         
               .WithCop        yO        bj        ectS
        eObjecArgs)
      
         
                
         
        .WithBuck        t(destB        cket
        Na        e)
              
            
        des
        Ob        ectNam        e)

         
                       .        i
        thTagging        Ta        ging.GetO        bj        ec
        t
                                   .Wi
        hReplaceT        ag        si        re        cti
        e(true);
                  a        ai         min
        io.CopyObjectAsy        nc        (co        py        bjectArg
        s
        ).Co        f
        gureAwait
        (
                  va        r g        et        bjec
        t
        T
        ags
        A
        r
        gs        t
        rgs()
         
                                                      .
        W
           
                                   .
        Wi        th        bject(d        estObject
        Name);
                   var 
        t
        et        bj        ect
        agsAsyn
        (get        bjectT
        gsArs        ).        onfigu
        eAwa        t(        false)
        
     
                      Assert.
        I
        sNot
        N
        tags);

         
           
         
                   var co
        p
        Ta
        g
        s();
                   
         
          
         
           Assert        Is        N
        tNul
        l
        tags
        )
        ;
        
          
         
         
         
        tNull
        (
        )
                   ss        ert.IsTrue(co
         > 0)
                        
         
          Ass
        e
        t.IsNotNul
        l
        (
        copiedTag        ["key
        1
        "]);
          
                  rt.Is
        rue(copi
        e
        dTags
        [
        key1"        .Contains
        ("        Co        pyObje        ct        ags"));
        

                                        
         
           new Mint
        "o        ject_Tes
        9", co
        yObje
        tSig
        ature, "Tests wh
        e
        ther CopyOb
        ect p
        as        , TestStatus.
        ASS,
    
         
                
         
        Dat
        eTime.Now - s
        artTime, a
        g
        : args).Log()
        ;
        
 
         
              }
     
          catch (E
        c
        ption ex)
        {
        

          
         
                 new 
        intLogger("Cop
        O
        ject_Test9", 
        c
        op
        y
        ObjectSignatu
        e, "Tests whet
        e
         CopyObject p
        a
        ss
        e
        s", TestStatu
        .FAIL,
    
         
                 Date
        Time.Now - st
        rtTi
        e
         ex
        Message, e
        x
        .ToStr
        i
        g(), a
        rgs: args).L         
            throw;
 
         
             }
   
         
                   
           {
       
         
           File.De
        e
        te        )
        
            awa
        i
         TearDown(mini
        ,
         b        o
        figureAwait(fals
        e
        ;
            
        wait TearDo
        wn(minio, destBucketName        wait(
        alse);
   
         
            }
        

           }

        #endregion

        #region Encrypted Copy Object

        internal static async Task EncryptedCopyObject_Test1(MinioClient minio)
        
        D
        teTime.Now;
                     r        e
         = GetRand
        o
        mName(15);
        

        =         etRa        domOb
        j
        ectNa        me        (10)         v
        ar destBucketN        m
        e = GetRan
        d
        d
        es
        ar        outFileN        ame =         "o
        ut        Fi        leName";
   
         
         
           v        ar n        str
        ng, str
        n
        >
 
                       e
         
              
         {         o        ,
        
         stB
        ckee        ketName },
      
         
         
         
        destO        bjectN
        a
        me },
    
         
        }
        ,
    
        }
        
        };
        
                     
         
         try
  
         
         
         
        y with         SE-C -
        >
         SSE-C encryption
        

                                            await 
        S
        etup_
        T
        es        k
        eAwa        t(false)
        

                    
              awa        it         
        S
        etup_T
        e
        t(mini        o
        u
        eAwait
        (
        alse)        ;
                  
          us        n
        c
        rea
        e();
                            aesEn
        r
        pti
        n.        Ke        yS        ize = 256;
                          
        ion.
        G
        ener        ateKey(
        )
        s
        ec = n        w SSE        C(        a
        
          
                         va
         sse
        py=         new SS
        Copy        (a        esEn
        rypt
                    
           using var d
        s
        Ae        sE        n
        ryption         =         Aes.C        re
        a
        t
        s
        tAesEncryption.y        ize = 256;

         
        r
        yption.Gen
        e
        rateKey();
            
         
         SSE        (destAesEn
        c
         
                      using 
        (
        var fil
        e
        stream =         r.        m
        See        (1 * KB))
                  
         
        {
  
         
                  var p
        tObj        ec
        t
        Args = new PutO
        b
        jectArgs()
                      
         
                            .Wi        tu        )
 
                                                          .        Wi        th
        b
        ect
        objt        me)
        

         
        .W        i
                            
         
               .Wit        hObj
        e
        ct        eamL        et                            .With        e
        rverSideEnc        yptio
        n
        (
        ssec);
               
         
             
         
        Object
        As        yn        c(put        Oj        ctAr
        g
        s
        t(f
        lse        )
          }        

         
         
         
         
        ourceO
        b
        ject        rgs =         n        w Cop        yS        o
                                          cket(b
        u
        cketNa
        m
        e)          
         
        .
        ctNa        me        

         
                                               .WithSer
        v
        erSid        eE        ncry
        
     
         
                     v
        a
        r cop        yO        bjectA        rg         = n
        ew CopyObjec        t
        Args()
         
         
         
         
        ctS
        urceo        ourceObjectArgs)         
                                     .Wit        Bucke
        t
        destBucket        Na        me)

                          
                   .Wit
        hObject(destObj        ctN
        a
        With        Serve
        r
        Sid
        E
        cr        ption(s
        s
        cD        t)        

            
         
         
           
         
         a        w
        p
        nc(co
        y
        Ob        ec        Args)
        Co
        nfigureAwai
           
        ar getObj
        e
        ctArg         =        new GetObj
        ectA        rg        s()        
                               .
        W
        thBucket(des        Bu        etName)
  
                                                  
           .WithObjec        (
        dest
        O
                        .
        W
        ith
        e
        ver        ideEn        ry        ion        ssecDst
        )
                             
                                 
         
        .
        W
        th        Fil
        e
        outF
        i
        l
        eNa
        m
        e
        )
        await
         
        j
        tObject
        i
        );
 
         
                               
         
         new MintLo
        g
        opyO        bj        t_Test1"
        ,
         copy        bj        ctSignatur
        e
        ,
        
                     
         
                     "T        s
        ypte
         CopyOb        je
        c
        t pas
        s
        s", TestStatus
        .
        P
        ASS, DateTime.
        No         - s
        t
        a
        sa        
                    .L        ;
        }
        catch (NotI        mentedEx
        eption
        ex)
 
            
         {
            new MintLo
        g
        ger("Encryp
        edCop
        yO        t_Test1", cop
        ObjectSig
        a
        ure,
   
         
           
                 "Tes
        s whether 
        n
        rypted CopyOb
        j
        ec
        t
         passes", Tes
        Status.NA,
        D
        teTime.Now - startT
        i
        me
        ,
         ex.Message,

                      
        e
        .ToString(), 
        a
        rg
        s
        : args).Log()
        
        }
   
         
          catch (Exce
        p
        ti
        o
        n ex)
       
        {
         
         
        new MintLogge
        r("EncryptedC
        pyOb
        e
        t_T
        st1", copy
        O
        bjectS
        i
        nature
        ,
                   
        hether encry
        p
        ed CopyObj
        c
        t         t
        tatus.FAIL, 
        D
        teTime.Now
        -
         s        .
        essage,
        
         
              ex.ToStr
        n
        g(        s
        .Log();
        
         
          throw;
     
         
         }        a
        ly
   
         
           {

         
                  .
        elete(
        o
        tFile
        ame);
     
               await TearDown(mi        ame).ConfigureAwait(false);
            awai        inio,
        destBucket
        N
        ame).
        C
        nfigureAwa
        i
        t
        (false);
     
         
          }
 
         
          }

        internal static async Task EncryptedCopyObject_Test2(MinioClient minio)
        {
 
                               var sta
        rt        Te        te        Ti
        e
        Now
        
   
         
         
        etR        domN        me
        (
        5);
                       
        v
        ar obj        ctNa
        domOb
        ect
        am        (1        );
               var
        d
        stB
        u
        cket        Na        m=        do        mName(15);
   
         
            var
        d
        stO
        b
         =         GetRa
        n
        domN        am        e(10);

         
         
         
        e = 
        "
        outFileNam        e
        Dictin        ar        y<s
        ring        ,         string
        

           
         
           {
            {         "buc
        k
        e
        N
        me
        "
        ,
         
        tNa
        e"         object        Na        me }
        

           
                               { "destB
        u
        c
        e
         },
      
         
             { "de
        s
        c
        tName        },                           { "
        d
         
          
        r
        y
                        {
    
         
               // Test C
        op         
        object         to unencrypted on         de        st        inat
        i
        o
        p_Test(        minio
        ,
         bucketName)        .C        ou        a
        i
        t(        als
        e
        );         
        minio,         de        stBucket
        a
        e).
        onfigua         
        using var 
        a
        es        En        cryptio        n         =
         
                   aesEn
        c
        5
        6;
                            aesEnc
                             
          var ssec = n
        w
        SS        EC
        aesEncrypti        on.K
        e
        y
        y =         ew S
        S
        ECopy(aesEncryption.
        K
        u
        sing (var         fi        estream = rsg.
        G
        e
        d(1         * KB))

         
                   {        
                            v        ar p        tO        jectArgs
         
        =         ne         Put
        O
        b
                   .i        uc
        k
        et(        uc        ketName)
 
                                        
        tN
        me)
                           
         
           
          .With        StreamD
        a
        ta                                    
         
           .WithObject
        S
        t
        h)
       
                                         nc        ry        ptio        (s        ec);

           
         
            awai        t
        (p        ut        bjectA        rg        ).Con        fi        gu        reAw        ai        t(        fl             
                      varc        oc        e
        c
        our
        eObject        Ag                .WithBu
        ck        et        bucketName)
                          
        Name
                             .        Wi        t
        Serv        er        i
        deEncryption(s        se        Cp        y);
        

                
                   va
         
        op        yO        bc        s
         
         new
         C         
           
         
         
                  .WithCopyObject
        o
        urce(copySourceOb        ectArgs        
 
         
                  (de
        tBucket        Na        me)
        

                                       .WithObject(
        destObj        ctName)
                       
         
        eEncr        p
        ion(nul
        
               
           awai        t         min        io        CopyOb
        jectAsy        c(copy
        O
        bj
        ec        Ar        s).Con
        f
        igu
        e
        wait(fa        ls        e);        

          
         
               
         
         n        w
         GetObje
        c
        t
        Ag        s(        )
                    
            
         
         
           
         
         
        (
        ame)

         
                                                     
        .W
        i
        e
           
                           
        .
        WithFile(out        ileNa        e)        
              
             awa        it         minio.        Ge        ObjectA        sy        ).Config        reAwai
        (false        );          
                        ne        w
        MintLo
        gger("En        rypte
        d
        Copy
        Oj        ec        _Test2
        "
        , co        yO        jectSigna
        t
        re
        ,
              
         
        wh
        e
        ther e        cr        y
        p
        t
        e
         Cop
        y
        bject        p
        ass
        e
        s
        "
        SS, D
        a
        - startTime, ar        s:
         
        ();

                       
        }
                       c        tch
         
        (
        Excep
        ion ex)

         
             
         
        {
        
         
         
          new MintLogg
        e
        r("En
        c
        ry        ct_Te
        t2", cop
        y
        Objec
        tS        gnature,
                       
         
                               "Tests w        he        th        e
        r enc
        r
        y
        j
        ss        estStatu
        .NA, D
        teTim
        .Now
        - startTime, ex.Message,

         
                   
           ex
        .T        ing(), args: 
        rgs).Log(
        ;
                
        }
        
  
              catch (
        xception e
        )
                {
   
         
          
         
             new Mint
        ogger("Enc
        y
        tedCopyObject_Test2
        "
        , 
        c
        opyObjectSign
        ture,
        
         
             "Tests w
        h
        et
        h
        er encrypted 
        opyObject pass
        s
        , TestStatus.
        F
        AI
        L
        , DateTime.No
         - startTim
        ,
        ex.Message,
 
                     
         ex.
        o
        tri
        g(), args:
         
        args).
        L
        g();
 
                   t         
        }
        fi
        n
        lly
      
         
        {
        F
        le.Delete(ou
        t
        ileName);

         
                  t
        TearDown(minio, 
        b
        cketName).Conf
        g
        ur        )
        
            awa
        i
         TearDown(mini
        ,
         d        e
        .Confi
        g
        reAwa
        t
        (f         
         }
   
         }

        internal static async Task EncryptedCopyObject_Test3(MinioClient minio)
        {
   
          
         var star        tT        ia        Now;
 
         
                      va         bucketNam
        e
         
        = GetRandomNa        me        (
        1
        5);
                    
         
        domOb
        j
        ctName(10);                   
         
                  var destBucke
        t
        Name 
        =
         
        15);

                              var destObje
        t
        ndomNa        m1         o        u
        tF        utFile
        rgs
        = ne
         
        ic        io        ary<s        ring, string>
        

                                       
         
         { 
        bu        ck        etNam
        e
        ", bucketName
         }        ,         bj        ectN
        m
        e",
        objectName
        }
        
  
                            {         "
        d
        e
        t
        uc
        k
        e
        ke           
         { "destObjec
        m
        ", 
        estO        bj        e          
         { "data        ",         "
        1
        KB" },
   
         
                     
         
        

                            //         T
        e
        st Copy of
         
        t
        o unencrypt        eo        es        t
        i
         
         await Setup_Test(minio,
         
        buck
        e
        t
        (fals
         
                   aw
        a
        i
        t Setup_Test(m
        i
        nio, 
        d
        e
        t(f
        lse);
                  i        g va         aesEncrypt
        i
        on        

                           
         
        aesEncrypt
        i
            
                  n
        .Gen        rateKey()        ;
          
         
         var s
        s
        e
        En
        ion.Key);
   
                    ar sseCpy = ne
        w
         
        .
        Key);
                           var 
        s
        ses3 = new SSES3();

        

        u
        sing (var 
        f
        ile        tream = rsg
        .n        e
        ed(1 * KB)
        )
                    {

         
        ectArgs          ne
        w
         Pu
             
             
         
              .        WithBucke
        t
        (buck        et        Nam        e)
    
         
         
                                                    .Wit
        h
        Objec
        t
        (
                         .Wi        th        St        rea
        m
        D
         .        hOb
        j
        e
        ng        t)         
        it        hServerS
        i
        deEncryptio
        n
        (
             
           aw
        a
        it minio.PutOb
        j
        ectAsync(putO
        b
        j
        ectArgs).Confi        u
        reAwa
        i
        t
                      }

              
         
             var c        op        ySour        ceObjectArgs         =         
        ew CopySourceObj        ec        Arg        s
        Wit        hB        ucke
        (bucke        tN        am        e)                                   
                  .Wit        hO        bj        e(        ec
        t
        am        e          
                 
         
         .Wi
        t
        (
        s
        s
                var cop
        jc         new C        pyO
        je
        c
         
        opy
        bjectSour        ce        (
        c
        opyS        ou        rceObjectArgs)
               
                        .        WithBu        cket(destB
        u
               .WithOb        j
        ct(d        es        tj        tm        
  
                             .
        Wi
        t
        Se        verSid        E
        ncry        t
        on(sses3)
        ;
          
         
               
         
        ect        A
        sync(cop
        y
        O
        b
        ectA
        r
        .Co
        n
        f
        igu
        re        w
        

         var 
        et        bjectArg
         =        ne        c
           
           .W        thBuc
        k
        et(de        tB        cketName)
                                       .        WithObject(des        Obje
        c
            .With        Fi        le(        ou        tF
        leName);

                 
        wait         in        o
        GetObjectA
        sy        nc        (ge        tO        b
        ectArgs)
        .
        Con        g
        reAwait(f        ls        );
        

               
         
        "E
        nc        yp        edCo        y
        O
        b
        j
        ct_T
        e
        ", c
        o
        py        Ob        e
        c
        t
        S
             
         
        "Tests whethe        r         ncr
        bj         T        st
        S
        tatus.
        P
        ASS        ,         ateTime
        .
        N
         args:        args)
  
         
             
                              .Log()
        ;
        

                               }
      
         
         catc
        h
        
                      {
     
         
                     ne         MintLogger("En        r
        yptedC        op        Object        Te        s
        t3", 
        c
        o
        g
        ,                 
          "Tes
        s whe
        her 
        ncrypted CopyObject passe
        s
        ", TestStat
        s.FAI
        L,        eTime.Now - s
        artTime, 
        x
        Message,
        

           
                     
        x.ToString
        )
         args: args).
        L
        og
        (
        );
          
         throw;
  
         
           }
        finall
        y
        
 
         
              {
     
              File.Del
        t
        (outFileName)
        ;
        
 
         
                  awa
        t TearDown(min
        o
         bucketName).
        C
        on
        f
        igureAwait(fa
        se);
      
         
           await Tear
        Down(minio, d
        stBu
        k
        tNa
        e).Configu
        r
        eAwait
        (
        alse);
        
        }
    }

        internal static async Task EncryptedCopyObject_Test4(MinioClient minio)
    {
        a
         s        ta            rt        im             =         Da            ti             va
         
        b
        t
        andomName(5        );                       var object
        a
        m
        b
        ectNam
        e
        10        );        
 
            

                               va
         des
        Ob
         =         
        etRandomNam
        (1
        );
           
        me = 
        outFileNam
        e
        ";
  
         
          var         a        r=         
        Dicti
        o
        n
        rin        g>        
          
             {
   
         
                               
                  "b        uN        }
        ,
        
            
         
                  tName
        , o
        jectName },        
           
         
           
         
          { "d
        e
        s
        t
        tBucketName         
        tObjectName",         d
        estObjectNa
        m
        e
         
        { "
        at        a"        ,K        },            
         
           
         
         
        1KB
         }
   
         
          }        ;
                    {
        

                   Tet         C        opy 
        f
        SSE
        S3 en
        c
        r
        yj        o         SSE-
        3
         ond        es        tination 
        i
        e
           
                 await Setup_        Te
        s
        t
        m
        ni
        o,         b        o
        );
    
         
                             wa
        k
        etName).Co        nf        i
        gureAwait(
        f
        s
        ses3         =         new S        SE        S          
        sseDest =         ne        w;         
         using         (         il        estr
        e
        g.GenerateS        tr        eamFromSeed(        1         *
         {
                    
             
         
             v        ar         putObje
        c
        tArgs         =         n        ew PutO
        b
        j
        ectArgs(        )
                                u
        
  
                         .Wi
        h
        ec
        (objectName)
                 .WithStre        m
        Data(files
        t
         
               .Wi
        t
        hObjec        tS        ze(
        f
         
                             th        Se        rrSideEncrypt
        on(s        es3);
  
         
           
                      awa        i
        ject        sync(putObj
        ec        tA        rgs).ConfigureAwait
        (
         
         }

      
         
             v        ac        A
        rgs = ne         C
        o
        pySourceObject
        A
                    .        Wi        hBucket(buck        etName)          
         
             
                   
        ame)
            
         
                                                  .With        Se        v
        erS        id        eE        ncry        ptiol           
          var copyOb        c
        Arg
         = new C
         
                                    
         
        .WithCopyObjec
        tS        e
        ObjectArgs
        )
        
                          u
        cket(des
        t
        Bucke        Name)

         
         
        .Wit
        Object        destObjectN        ame)
        

                             
                  .W        ithServ        rSideEn
        c
        rypti
        o
        n
                    await minio.        Co        py        Ob        ectAs        yc        (copy        Ob        jectArg        s)        .C        o
                   var getOe        ctArg
         = new
         GetObje        ct        Args(
        )
                     
         
                         .W        iB        ucke
        t
        ame)
        

        t
        hOb
        j
        ec        testObjectN
                      
        m
         aw
        it minio.G
        e
        tObjec        tAsync(getObjectArgs        ).
        Conf        ig        ue        at        f
         new         i
        tLog        er(
        cryptedC
        pyObject_T
        st4",         co        pyObjectSign        t
        u
        re,
         
         
                
         
           
         
                  Tests w
        h
        er        encrypt
        e
        T
        e
        stStatus.        AS         Dat
        e
        ime.
        N
        ow         -         st        ar        t
        T
        s)
  
         
          .Log();
                
        t
        ex)

         
                              

                   
         n        e
        cryp        e
        CopyObje
        c
        t_Te        t4         copyObjec
        t
        Si        gn        atu        re        ,
        
         
                     
         
        "
        ncryp
        ed Co        pyOb
        j
        ect p
        a
        se        ", Te        tS        atus        F
        A
        I
        L, DateT        im        e.Now -         s        artT
        i
        m, ex.Messag                          
        x.ToSt
        ing()
         arg
        : args).Log();
          
         
         throw;
   
            }
        
            finally
  
             {
  
         
               F
        i
        le.
        Delete(outFil
        Name);
   
         
              await T
        e
        ar
        D
        own(minio, bu
        ketName).C
        n
        igureAwait(false);

         
          
         
                await
        TearDown(minio
         
        estBucketName
        )
        .C
        o
        nfigureAwait(
        alse);
       
        }
            }

        #endregion

        #region Get Object

        internal static async Task GetObject_Test1(MinioClient minio)
        {
        
        var sta        r
        .
        ow;
                a
         b        cketName
        =
         
        ;
                                                 objectName 
        jectName(10
        );
                string         c        o
        y
        e          nu
        ll;

         
          
            
        F
        ile        m
         
        vg        ny        ing>
                
                       
          {         "        bu        cketN
        me", b
        ck
        tNa
        me        },                    {
        bj        c
        Name 
        ,
                                   {         con        t
        e
        tType", co
        n
        t
        en
        ;
        

                                awai
        t
         
        Setup_        Test(mini
        o,         k        ur        eAwa
        t
        fal
        e);


         
         
                  g (a        r files
        r
        am 
         rsg.
        G
        e
        n
        mSeed
        1
         * 
        B))
                       
        {
        
                var f
        i
        l
        _i        z
        h;

                                       
        o
        g f
        le_re        ad        _s        ize = 0
        ;
        
         u
        tObjec
         
                                         
            .WithB
        uc         
                               
        .
        WithOb        j
        )
        
                               
                   
        e
        am)
                           .
        W
        ithOb
        j
        e
        ngth                  
         
         .WithC        ontentT
        y
        p
        e(contentType);        

         
        tOb
        ectAsync(putObject        Arg
        )
        Con
        ig        ur        eAwait(false);
        
         g
        etObjectAr
        g
        s = new Ge
        t
           
         
        t
        Name)
                           
         
        .Wit
        h
        O
        Nam
        )
                             
           
        W
        r
        eam, cancellat        ionToke
        n
        ) =>
                                            
                         va
        r
         fil        eS        tream = Fi
        l
        t
        empF        leName
        )
        ;
            
         
        e
        am.CopyToAsync(fi        le        Stream        ,         ca        ken).
        onfig
        u
        reAwait(false);
        

                      
         
         
                   tre
        m.DisposeAsyn        )
        Conf        gureAwait(fal
        se        )
         
              var 
        w
        rittenInfo         =        new
         n        Fi        leNa        me        );

         
                   
        _z        tte        In
        o.Len
        g
        th        ;
        
           
         
                    A
        s
        se        rt.AreEqual(fi
        l
        ew        _si
        e,         ile_rea
        d_        ize);
                                 
             File.Delete(tem        pF        il
        e
        });
                         
             awa        in        o.        Ge        tObjectA
        s
        ync(
        g
        tObj        ec        Arg
        s)        .C        on
        i
        ureAwa        i(        l
        s
         
         }

        

         
         
        kD        00
        )
        w
                   tLogger        "GetObjec        t_        Test1",
         getObjectSig        na        ture, "Te
        s
        as        m work        ",
                                    
                   T        stS
        tatus.PASS, Date        Ti        me        .
        N
        w - star
        t
        Tim
        ,
        args: a        rg        s)
        .
        og
        (
        );
    
         
        (
        E
        xcept        on 
        e
        x)                     
        {
  
         
         
           
         
         
         
        r("Ge
        t
        t", getObjectSign
        s
        bj        ec        t 
        as         strea
        m
         works"        
   
         
         
        Statu
        .F        AI        L,         Da        teT
        i
        me.No
        w
        - s        artTime
        ,
         
        ex        Me        sage, ex.T
        o
        Strin
        g
        (
        g();
                 
         
         thro
              }
                        f
        in        al        ly
        {
        
             
         
         
        le        ts(        FileName))
                    File.Dele        empFileN
        me);
 
             
            
        wait TearDown(m
        i
        nio, bucket
        ame).
        Co        ureAwait(fals
        );
      
         
        
    }

        internal static async Task GetObject_Test2(MinioClient minio)
              var
         
        sta        r
        tTime = DateTime
        No                    
         
        ar         bu
        c
        5)        ;
    
        obj
        ctNa
        e
        = G
        tRandomObj
        e
        ctName
        (
        0);
  
              var f
        a
        domName(1        );
        
                       ar a        rg
         
        =         ry        strig        
   
                   
        { "buc        ke        Name"
        , buck        et        Na        me },
 
                  {
         e        je                      
         { "fileNa
        m
        e", f        i
        l
        Name }
   
         
         
           };
               tr        
            
         
         
        ait S
        t
        up
        Test(m        ni        , 
        etN
        am        e)        .C        of        gureAw        it        f
        e
        )
        ;
        sn         = 
        sg        .G        en        rateStreamm        1
         
        /         o
        g         ile_write_si
        e
        =         fl           
                        v
         
                           
        .
        WithBucket
        (
                     .Witb        j
        le        st
        re        .W        it        hObjec        tS        ifilestream.Le
        n
        t
         minio.PutObj        ec        tA        s
        y
        nc(putObjec
        t
        A
        wait        false);

         
               }
                      catc         (Exceptio
        n
         ex)

         
                    n
        w         MintLogger(        "G        e
        O
        jec
        _Te        s
        p         for 
        G
        e
        ",
                                          
        ms        ta        rtT
        i
        e, ex.Message, ex        .T        o
        tri        g
        Dow        n(        inio, buck
        t
        ame        )i        ur        eAwait(false        );        

                    
         
         }

       
         
        try
      
         
        {
            var
         
        ge        Object        Ar        gs = ne
        w
         GetO
        b
        j
        hBuck
        t(bucketN        m
        e
        )
          
         
                    .WithObject(
        o
        bjec        tN
        a
        WitF        il        e(fileName
        ;
        
                   
        a
        wait minio.G
        e
        t
        nfigu
        e
        wait(false        ;
        

                                
         
        (f        il        eNam
        e
        ));
    
         
                               new MintL        g
        g
        r("GetObj        ect_Te
        s
        t
        ests
         
        whethe
        r
         GetObje        c         i
        l
        e
             
             
         
        TestStatus.        PA        SS,
         D        at        Time.Now -         s
        t
        a
        rtTi        e,        args: ar
        g
        s).L        o
        

        e
        c
        tNotF
        o
        undEx        ct                     
          new Mint
        L
        ogger("GetObject_
        Te        t2", getObjectSign
        a
        ure,         t        s 
        or Get
        bject
         w
                           
         
           T
        e
        tStatus.
        PA        SS         
        a
        eTime        .N        ow -        st        rtTi
        m
        , ar
        gs        :         rgs        ).        L
        o
                   }        
       c        at        c
        h
        p
         
         
                     {          
              new MintLog        tject_Test2", gt        Ob
        ectSig        na        ure, 
        "T
         with          
        file
         
        ame",
                    
         
                              
           TestSta        t
        FA
        I
        L, Date
        T
        me
        .
        No        w         - star        tT        iex.Message
        ,
         
        e
        rgs:a        g();
                    throw;
    
        at
        c
        h (Exc
        e
        ption ex)
  
         
                   new
         
        MintLo
        g
        ger("GetOb        ec
        t
        _
        Sign
        ture, "T
        e
        st        s         for
         
        etObjec         wi
        t
        h         a file name",

         
                      
         
        .A        DateTime
        Now - 
        tartT
        me, 
        x.Message, ex.T
        o
        String(), a
        gs: a
        rg        og();
       
            throw
        

               }
        

           
             finally

               {
 
         
                if (F
        i
        le
        .
        Exists(fileNa
        e))
      
         
               File.Delete(
        f
        il
        e
        Name);
      
             awa
        t
        TearDown(mini
        o
        , 
        b
        ucketName).Co
        figu
        e
        wai
        (false);
 
         
              
        }
            }

        internal static async Task GetObject_3_OffsetLength_Tests(MinioClient minio)
            // 3 tests will run to check different values of offset and length parameters
            // when GetObject api returns part of the object as defined by the offset
            // and length parameters. Tests will be reported as GetObject_Test3,
            // GetObject_Test4 and GetObject_Test5.
        a        r         startTime =         t        .
        Now;
     
         
          ve        Name(        15);
        
         t
        Name = GetRand
        o
        mObjectNam
        e
        (10        )                      st
        r
        ing contentT        yp        e 
        =
         null;
                     t        leNa
        m
        e = "
        t
        e
        GetRandomName()
        

                v        ar        te
        m
        te        -"        +
        GetRandomN
        a
        me()        
                      vars        c
        ionr        yt        ri        ng
         {
        

           
         
              /        /         i
        s
         i
        s
         {offse
        t
         l
        e
        ngt        } val
        u
        e
        s
            
         
            
         
         
        { "e        est3
        "
        , new
         
        ist<i        t>        { 1
        4
        ,
         20 } },
     
         
             
         
        {
        "
        nt>
        },         { 
        tObject_Test5
        ,
        new         L        st<in        t>         25 } }
                ;
        

                fo        re        a
        s
        etLengthTe
        st        s)        
                             va
        r
         testName        =
                      
        var o
        f
        fs        et        Tom = test.Value        0]        
   
         
             
         
         
        l
        ue[1];
                    
         
         
        = n
        w Dictio        na        y
        <
        string, string        >
                      
                                      { "b
        cket
        a
        e",         b        u
             { "ob
        j
        ectN
        am        ", objec
        t
        Nam
         
        ,
                                  
         
            
        {
        "co        nt        e
        n
        t
        Ty
                                                    { "offset", 
        offsetToSta
        g()
        },
                         
         
              { "len        gth",
         lengthToBeRead.T        oS        rin
        g
        ) }
              
                    ;
             
        
                          
                     a
        w
         S        tup_T
        e
        st(
        i
        io, buck        et        a
        e).        Co
        n
        igur
        e
        A
        wa        ia        ;
                                
         
        cha
        acters to 
        t
        est partial
     
         
                        // get ob
        jc        t.            v
        rl        ine 
        w[]
         {
        stuvwxyz1        234        56        7
        8
        " };
                                          
         
          //   ab
        c
        ef
        g
        hijklmno        p
        tw        yz0
        1
        3456
        7
        8
        9
 
         
         
                  /   0
        1
        456        78        9
        1
        2345        67        89312
        45
        

                  Chr
        10thChr        ,         ^20
        t
        hChr, ^30t        h         ^h        hr =>        char
        a
        ter        s'         se
        uen
                
                              
        /
        Exam
        le: 
        of
         
        the 
        e
        pected si        e a
        d
        ntent
          
         
         
         
               
        /
         g
        e
        tO        bj        ectAsy
        n
        c
         
        il         r
        e
        ur        n a
        r
        e
         4 
        a
        n
        d
        tivel
         
        File.WrteAllLines
        e,
        l
        in        e)        .Co        nf        igureA
        w
        ait(fal        e
        )
        ;
        sing
         
        (var f
        il        es        tre        am         = F
        i
        l
        rce,        Fi        eMode.Op
        e
        n, Fi        le        Ac        es        s.Read, F
        i
        l
        eSh        re.Re        d)        
            
             
         
         
                            var obje
        tSize 
         (int
        file
        tream.Length;
                
         
           var expe
        tedFi
        leSize = lengthToBeRead;
                    var expectedContent = string.Join("", line).Substring(offsetToStartFrom, expectedFileSize);
                    if (lengthToBeRead == 0)
                    {
                        expectedFileSize = objectSize - offsetToStartFrom;
                            var noOfC
        rlChars =
        1
        
       
         
           
                     
        f (Runtime
        n
        ormation.IsOS
        P
        la
        t
        form(OSPlatfo
        m.Windows)
         
        oOfCtrlChars = 2;


         
          
         
                        
           expected
        o
        tent
         = string.Joi
        ("", line)
 
         
                   
         
                  .Su
        b
        s
        tring(offsetT
        StartFrom,
        e
        pectedFileSize - 
        o
        fCtrlChars);

         
         
                     
            }

          
         
           
           long ac
        t
        ualFil
        e
        ize;
        

           
         
                            bjectArgs = new PutObjectArgs()
            
               .WithBucke
        t
        buc
        etNa
        m
        e)

         
         
          
         
          
         
         
                  h
        bject(objectName)
        

           
            
         
           
         
         
          
         
         
         
        W
        it        f
        lestream)
       
         
           
            
         
           
         
         
        W
        i
        hO
        j
        ctSize(obje
        ctSize)
         
         
           
            
          
         .WithContentType
        (contentType           
               a
        a
        t mi
        n
        io.
        Pu        (pu
        ObjectArgs).Confi
        u
        eAwa
        i
        t(fal
        s
        e
        )
        ;
           
             var getOb
        e
        tArg
        s
         = ne
        w
         
        G
        et        
  
            
         
           
                  
        .
        WithBu
        c
        et(buc
        ke                   
        WithObject(o
        b
        ectName)
 
         
                   
         .WithOffset
        A
        dLength(of
        s
        et        g
        hToBeRead)
  
         
                   
         
                  c
        Stream(a
        s
        nc (stream, cance
        l
        lationTo
        k
        e
        )
         =         
                
        {
                      
         
                
         
         
                  a
        m         e(te        
              
             await
         
        strea
        m
        CopyToAsyn
        c
        (
        fileStream, ca
        n
        cella
        t
        ion        Await(false);
                            await fileStream.Disp        gureAwait(false           
            
         
           
         
        v
        r
        writtenInfo = new FileInfo(tempFileNam
        )
        ;
                    actualFileSize = writtenInfo.L                            Assert.AreEqual(expect        alFileSize);

                            // Checking the content
                    var actualContent = (await File.ReadAllTextAsync(tempFileName        ken)
                                    .ConfigureAwait(fals             
            
         
                 .Replace(
        "
        \n", "")
 
         
            
         
         
                      
         
             
         
         .        );
  
         
           
                  
         
            
         
          As
        s
        ert.AreEqu
        a
        (actualC
        o
        nten
        t
         expectedC
        o
        nten
        t
        ;
       
         
            
         
                            t m
        nio.GetObj
        c
        A
        syn
        c
        (getObject
        A
        rgs).C
        on           
                    }

 
         
                    ne
        w          ge
        ObjectSignature
         
        Tests 
        w
        heth
        e
        r 
        G
        tObj
        e
        c
        t returns
         
        all the data",
  
         
                        
        T
        es        me
        N
        ow - startTime
         a
        g
        s:                  (Exception ex)
 
         
                {

         
                     new 
        Mi        jec
        Signature, "T
        s
        s
         w         a
        l
         the data",
      
         
                    
         
        TestStatus
        .
        FAIL, D
        a
        t
        Time.Now - st
        r
        T
        ie        g(), args: args
        .
        og();

         
            
         
          
         
            
                   
            final
        l
        y
            {
 
         
                     if 
        F
        le.Exists(tem
        p
        Fi        (e            
            if (File.E
        xi        e.D
        lete(tempSour
        e
        ;
 
                     
         
        aw        e
        tName).Con
        f
        igureAwait
        (f         
            }
    
        }

        internal static async Task GetObject_AsyncCallback_Test1(MinioClient minio)
        
        ta        rtTime = Date
        T
        im        .Now;
  
         
        e(        15        ;
                      var
         
        objec        tName 
        =
         
           string conten        t
        Type = null
        ;
        

         G        tR        n
        omN        am        e(
        10        );
        var
         
        des        FileName =
         
        G
        etRand        mN        me(10        );        

         
                               v        ar         
        tri
        g, str        ng        
                       
        
           
                      { "bucke
        t
        N
                  { "obje        ct        am
        e
        ", o        bjectNa
        m
        Type", con
        t
        en        tT        pe }
  
         
         
        {
                           // Crea
        t
        e a        la        rge local fil
        e
                           if 
        (
        r
        m(OSPlatform        Window
        s
        ))         G        ne
        a
        teRand
        o
        File(f        il        eName);
                                         
        "
        e t
        e bucket
          
         
            
         
          awai
        t
         Setup_Test(
        m
        i
        t(fal
        ;

                           using         v        ar         file = Fi
        l
        .Op        en        Handle        (f        ileName)
        ;
        

                    us
        i
        ng va
        r
         f        ile, 
        ileAccess.
        R
        ead);
      
         
         
         
           // Upload t
        h
        e lar
        g
        e
        
                          var 
        i
        e =
        filestre
        a
        m.Length;
                              ;
            
        a
         putObjectA
        r
        gs = n
        e             .W
        i
        thBucket
        (
        bucketName)
    
         
                  .Wit
        h
        Obj            .WithStreamData(file        hOb
        ectSize(files
        r
        a
        m.Len
        th)

         
                       .
        W
        ithContentTy
        p
        (contentType);

 
                  p
        utObjectArgs).
        C
        onfig
        u
        r
        b
        ackAsyn
        c         =         a
        s
        ync 
        (S
        o
        nToken)
         
        =>
 
         
          
         
         
        t =         n        ew 
        F
        ileStre        am        d
        estFileN        am        e, Fi        le        de.Create,         F        ileAc        ce        s
        t
         
        sr        , can
        el        ati
        o
        nToken)        Co        fi        gure        Aw        a
        it(false);        
            
         
         
                      };

     
         
             
         
        v
         
                    
                  
         
        .WithBuc
        k
        t(b        cketName)
     
             .        Wi        thObj        ec        to        bj        ctNam        e)        
          
               
           
        Wit
        Call
        ba
        ca        ncellatio
        n
        Toke
        n
         =>
                         
         
               awa         c        ll        b
        a
        kAsy
        n
        c
        (st
        r
        e
        am        on        false
        )
        ;

      
          
         
        e
        ectr        gs).Config
        u
        reAwait(
        f
        lse);
                   v
        a
         wr        tt        nI        fo = ne
         FileInfo
        destFil
        Na        e)        
  
            
          
        enInfo.Leng        th        ;
  
         
                                       Ass
        e
        rt.
        r
        Equ        l(        iz        e, 
        f
        le
        _
        read_si
        z
        );
        

        
       
         
         
         
         new
         
        intLo        ge        ("
        G
        e
        t
         getO
        bj        ,
           "Te        st        s         b
        s"
                    
         
              
         
          Te        st        Status.P
        A
        S
        , Da
        t
        eTime.
        N
        ow - startTi
        m
        e
        );
         
            
         
                  }
   
         
            catch 
        (
        E
        cept
        i
        on        ex)

         
               {
                   
        r("Ge
        Object_L
        a
        rgeFi
        l
        _Test0        ", ge
        t
        O
        bjec        tS        ignature, "        T
        ests 
        w
        he        c
        m
        "
                
           Tes
        Statu
        .FAI
        , DateTime.Now - startTime, e
        x
        .Message, e
        .ToSt
        ri        , args: args)
        Log();
  
         
               t
        h
        row
        ;
        }
 
              fina
        l
        
        {
  
         
          
         
              if (Fil
        .Exists(fi
        e
        ame))
             
         
          
        F
        ile.Delete(fileN
        me);
      
         
           i
        f (File.Exist
        (destFil
        N
        me))
        
         
          
         
            File.Dele
        e(destFileNa
        e
        ;
           
         
        aw
        a
        it TearDown(m
        nio,
        b
        cke
        Name).Conf
        i
        gureAw
        a
        t(fals
        e);
        }
    }

        internal static async Task FGetObject_Test1(MinioClient minio)
        {        
         arT        i         e.        N
        o
        w;
       
         
        var buc        ke        t
        N
        me =         et        RandomN        am        (15)
        ;
        
       
         
        v
         n        O
        je        t
        ame(10
        tFile        Na        e = "outFi
        le        Na        m"         
         
          vr         r        tiona
        y<s
        ring
         
        tri        ng        

                {

         
                
                   "         bu        ck        et
        ame         }        
         
         
        { "
        bjectName"
        ,
         obj
        e
        ame },
  
                               
         
         

        tFileName }
                   
                    };        
          {

                   
                   await Se        tu        p
        _
        Test(m
        in        me).
        onfigureAwait(
        a
        s
        e
        us        g (var file        st        re
        m
        =         rs
        .GenerateStre
        a
        mF        

                                    {        
                   
         
        g
        s =         ne        w PutO
        b
        jectArgs()
        
          
          .WithBucket(
        b
        ucketName)
        
          
          .With        bject(o
        b
        jectNa        e)                   
         
                      
         
        Da        ta        (filestream)
                   
                   
         
         
        ize(f
        lestr
        e
        am.Length);
  
         
                             a
        w
        ai        t         minio.        Pu        te        ut        ObjectAr        g
        )
        Co        nf        igue        A
        w)                        
         
        }

        j
        c
        Arg         =         new GetOb
        j
        ec        tArgs()
   
         
                
         
          .Wit
        h
        ucket(buckm         
            .W
        i
        thObject(ob
        j
        ectN
        a
        e)
                                       .
        W
        i
        thFile(outFil        eN
        a
        me);

         
         
        m
        i
        (
        w
         MintLog        ge        (
        "
        FGetObject
        _
        e
        ctSignature, "Tests 
        w
        asse         for         sm         upload",
                                              
        me.No
         - sta        rt        ime,         ar        gs        :
         ar        gs        ).        Lo
        g
        );
    
         
         
          }
        ca
        t
        ch (Ex        ep        t
                  ("FGet
        O
        b
        ject_Te        st        1", get
        Ob        je        tSi
        g
        na         w        he        er FGetObje
        t
        pas        s for sma        l upload        ,
  
         
         
        tatus.
        F
        AIL, D
        a
        artT        im        e,
         e        .M        ssage
        ,
         ex.
        TS        tr        ng(        ),         args: a        rg        )
        .
        hro
               }

         
                                       
                     {
            
        Fl        e(outFileName);
             r
        D
        ).Configur
        e
        Aw        ai        t(        a
        se)        ;
             
                   }        
 
         
        }

        #endregion

        #region List Objects

        internal static async Task ListObjects_Test1(MinioClient minio)
        {              
          var         st        ar        tTime = DateTi
        m
        .No        w;
                       var
        bucketNae        eR        an
           v        r pref        x
         = "        i
        ix";
   
         
           
        v
        r objectN
        a
        e         =         p
        refix +
         
        et        a
        ndomN        ame(
        1
        0
        )
        
   
         
           v
        ar        arg
        s
         
        =
        <s        tr        ing,         string>
  
                      
        t
         }
        

                     
              
        {
         "o        je        tNa
        m
        e
                     
         
           { "
        p
        refix"         p
        r
        e
                   
        {
         "re
        c
        ursive        ,
         "false"        }
          
         
         
            
         
        {
            
         
              await        Se        u
        uck        et        Na
        e).Confi
        g
        ur        Awa
        it        fa        se);            
         
         
                      v        ar task        s          
        n
        ew Ta        s
        k
        [2];
                o        ar i = 0
         i < 2
         i++)
            
               {
       
         
                tas
        s[i] 
        =         bject_Task(mi
        io, buck
        t
        ame, obj
        e
        ct        Na        m
        e
         nu
        l, 0, n        ul        ,

         
                     
         
          
         r        s
        rF        omSeed(1));                        
         
                           a
        a
        t Task.        WhenAl
        l(
        igu
        eAwa
        t
        fal
        e);

             
         
                    aw
        istOb
        jcts_Test(m
        ,
        prefix, 2, f
        a
        se).Con        igu
        e
        A
         
                       awai
        t         Ta        k.De        la        (20
        0
        )
        
        
         
        L
        ogger("ListObjects_Test
        tsSig
        ature,        
             
         
                      
         
          "Tests w
        h
        et        er ListObject
        s         is        s         al        objec
        s
         ma
        ching a p        ef        x
        non        r
        ecursive", Te        st        Status.PA
        S
        S
        

          
         
                  e
        ar        g
        )
        Log
        );
                        }
  
         
         
        )
        
        {
        
                            
        n
        c
        ts_Test1",
         
        li        st        ObjectsS
        i
        ethe
        r
         ListObjec
        t
        ix 
        n
        on        recurs        iv        e"
        ,
         TestS
        t
        a
                               
        ateTi
        m
        e.Now - startT
        i
        me, ex.Messag        ,         x.ToString        (), 
        a
        rgs: 
        a
        r
         w                              }
         
           
          fina        ll        y
     
         
         
        T
        earD        ow        (mini
        o
        , bucketNa
        m
        t
        (f        al        se);
   
         
                    }        
            }

        internal static async Task ListObjects_Test2(MinioClient minio)
                  v        r startTim
        e         =         DateTime.No        w          v        ar bucketNa
        m
         = GetRa        na        e(15)
        
  
             
        ar arg
        s 
        in
        g
        >
          
           {
    
         
            
                   "bu
        c
        k
        etN        am        "
        ,
        }
        };
                    
         
          try
           a
        ait Setup_
        T
        est(mi        ni        o,         bucketNam
        e).Conf        igureAwai        t(        false)
        ;
        
            await ListO        je        ts_Tes
        (mi
        io, b
        cketNa
        me
        igure
        Aw        ai        t(f
        a
        se        )           await
         T        sk
        .
        Delay(20        00        .C
        on        figureA
        w
        a
        i
        (fal
        s
        );
 
                            w MntLogger("
        _Test2", listObje
            
         
              
         
            "Tests 
        w
        he        ects 
        asses                  c
        k
        et is emp        ty        ", Te
        s
        tSt        tu
        s
        .
        T
         -         tTime,
                    args: args).Log                
        
     
          cat
        h (E
        ception ex)
     
         
          {
       
            n
        ew        t
        stO
        jects_Tes
        2
        , listO        bj        c
        tsS
        ig        
                     
         
        Tests h        ist
        bjects         a
        ses         he
        n 
        pt        "
         TestSta        tu        .
        A
        L,         Date
        i
        ow -         startTim
        e,        
                    
        x.        Me
        sage        ,
        .String(), 
        args: 
        a
        gs).Lo
        g
                  i
                         awa        it         
         
        uc        ke        tName
        ).Con        fi        g
        se        ;
                        }

                   }

        internal static async Task ListObjects_Test3(MinioClient minio)
        {
                        var         st
        a
        rtTim
        e
         
           
            v
        rb        uc        ke
        Name         =         
        G
        e
        tR        );

         
           
         
        v
        r         e
        i
         =        m
        in
        i
         
         + "        "
         
        +
         e        t
        andomName(10) 
        +
         "/        uf
        f
        x";
                      
        v
        r ar        gs         = new         c        ion        r
        y
        s
        t
        ing,
         
                   "bucketName"
        ,
         
        b
        u
        c
                  ct        am        "
         obj
        e
        ctName         }
        ,                        {         pr        fix"         
        p
        r
                    "recu        rsive", "t
        r
        u}                     };

         
         
         
                   t        y

         
         
              {
                   
        etup_        T
        st(m
        i
        nio, 
        b
        uck        tN        a
                       
         
        var tasks = new Tk                           
        f
         i++)
                          
         {
                 
             
        tas
        s[i] =        Pu        Obj        ct_Ta
        k
        minio,         uck
        e
        tN        ame, obj
        ectName + i, null        ,         n
                     rate        tr        amFr        m
        Seed(1 * KB
           }

     
        sW        nA        ll(tasks
        ).
        figureAwait(f        a
        it Li        st        bjects_Tes        t,        , r        ef        ix
        ,
         2).Confi
        gu        re        wait(f        al        s
        k.Dela        y(        2
        0
        00)
        C
        nfigure        Aw        ai
        t
        fa
        l
        se);
  
         
          
         
             new
         
        M
        it        Lo        gge
        r"        Li        sj        ", li
        stObjectsSi
               
        he        r ListO
        b
        jects         l        i
        ts all ob        jt        tc        h
        ing a
         
        p
        ec        v"        stStatus
        PASS,

             
            
            DateTime.Now 
        -
         startTime,
        args:
         a        .Log();
     
          }
              
         
        atch (Ex
        c
        ept
        ion ex)
     
          {
                        
         new MintLogg
        e
        r(
        "
        st
        ", l        i
        atu        re        ,
         
        t
        er L        is        tO        bjects
         l        i         l objects m
        a
        efix and recur        ta        us.
        AIL,
                       
         
             
         
         DateTime.        N
        o
        w
         - startTime, 
        e
        x.Mes
        sa        g
        ), ar
        s: args).Log();
         
                     
         
                  hrow;
                        fi        nally
        

                              
         
        {
        await         earDo        wn        (mini
        o
        , bu
        c
        k
        et        Name).Conf        ig        ur        eA        a
        it(f        al        e
        )
          }

        internal static async Task ListObjects_Test4(MinioClient minio)
        {
  
             var st
        Time 
         Dat
        Time        .N        ow
        
 
             
         var b        uc        ketName         =         e        nm        m
        (15);
   
                  e = 
        G
        tRan
        d
        o
        mOb
        j
        e
        c
        r ar        s
        =
         n        w Dicti
        na
        r
        t
        {
          
                      { "        ucketName"         b        c
        etName },
                            
        {
        tName
        },
    
                      { "re
        urs        ve"
         "fa        l
           
                   };
      
                  ry         
         
                     
           
         
            await         
        S
        i
        o
        , b        cket
        Nm        e
        ).Config
        u
        r
        ew        ait(
        fl        se        ;

         
         
                              = new Task[          for 
         < 2; i++)        

         
                       {

         
                            ss        = PutObj
        ct_Tas
        (mini
        , bu
        ketName, objectNa
        m
        e + i, null
         null
        ,         ull,
        
                                           r
        Se        d(1 * K        ));

                                          }

    
         
          
         
         
        .W        e
        All(        as        )
        Con        fig        u
         
           
        ai         Li        tObjects_
        T
        es
        tm        in        o, buc        ke        N
        am
        fa        s
        ).Co
        f
        gur
        Await(fals        e)        
                      
         
        k
        Delay(2000).C        o
        gu        re        Ai         
                  new        intL        og        ger("L
        st        Ob        ,
        listObje
        cs        Si        gu         
                 "T
        e
        ts         wh        et
        her ListObjec
        t
        bje
        p
        ", Test        ta
        t
        us        .P        ASS
        ,                          
        me,
        args:
        a
        gs)
        Log(
        )
        ;
        

         
         t        E
        xc
        e
         
        w Mi        t
        L
        o
        g
        e
        ("ListObjects        Te        t4", 
        l
        stObj        ec        tsSig
        n
        ture        ,
     
         
         
         
            
         
        T         s a
        l
        l objects when no p        refi
        x
         
        s
        spe        s
        IL
        ,
        
      
         
                       
         D        ateTime.Now
         
        - sta
        r
        ti        ge, e
        .ToString(), arg
        s
        : arg
        s
        .Log();
  
         
                                h
        r
        o
        w;
                }
  
         
            n        l
        l
        y
   
         
                            {

                                  await         Te        rDo        wn        (m
        i
        n
        onf
        re        Await(fa        l
        se);
        }
    
        }

        internal static async Task ListObjects_Test5(MinioClient minio)
        {
                
        a
        tt         = DateT
        ime.Now;
               va
        r
        domName(
        1
        5);                       var o
        bj        ctNa
        me        refi
        x
         
        = G
        e
        t
        R
        1
        var 
        u
        mOb        je        cts         = 1
        0;
        

        r
        cti
        nary<stri        ng        ,
         s        tg        
                          { "
        b
        uc        et
        ame },
                  
        { "
        bjectNa
        e"        ,b        ctNa
        ePr
        fix },
  
                  { "re        urs
        i
          };
   
         
                            va         
        bjectName
        s=         n        e
        w
         Lis        t<sr         
         
         tr        

         
            
         
         
        {
 
         
         
         
        tup_T
        est(minio, bucketName).Conf
        ls            v
        r tasks 
        =
         new         Ta        s
        [nu        mO        bj        ects];
        

         
                   fo        r 
        (
        var i         =1        et        ++)
    
              
        {
   
            
               var objNam
        e
         = objectNa
        ePref
        ix        ;
           
                   asks
         1]          Put
        O
        bje
        , b
        cketName, 
        b
        Name, null, n        l
        l,
         
        0
           
                  
                  rs        .Gene        ra        te        Stre        am        Fr        oe        )
        ;
                        
             o        jec        tm        .d        N
         
              // Add
         
        leep        to avo
        d
         
        ri        h conc        r
        r
         
                          
                   (i 
         
         
           
        k
        nfigue        A(        e)        ;
         
                          }
        

         
                                           await
         T        ask.
        W
        h
        fig
        reAwa
        t
        fal
        e);

        

         
         
                  t L
        _
        e
        s
        (
        mi
        n
         
        umObj
        e
        c
        t
        ,
        false).        onfigur        A
        wait(        a
        se)        
      
         
            await         T        a
        k
        D
        e
        ay(50        ).Con        f
        g
        u
        eAwa
        i
        nn        er("
        L
        i
        t
        bj
        e
        c
        t_        t
         
                       
         
           "T
        es        s wheth
        ll 
        bjects when n        um        ber         of ob
        j
        cts == 1        00,         Test        t
        a
        tus        PA        S,
              
         
             
         
         
        im        , arg
        s
        : ar
        g
        s
        ).o        g();
    
         
           }

         
         
        xce
        tion ex)
 
         
                     {
           
         ne         MintLogg        er("ListObj
        e
        jec        ts        ig
        ature        ,
 
                           T         whe        Ot        ll objects         wh        en
         
        numbe        r
        St
        at        s.FA
        IL        
           
                     
                             
        .Now 
         
        st        art        T
        e
        sage,         e        x.        To        tr        n
        g(), args: args).Lo
        g)        ;
                   th        ro        ;
  
         
             
         {
    
               awai
         Tear
        ow        n(        m
        nio, bu
        ketN
        me
        .Confi        g
        wa        t(false);
        
     
                    }

        internal static async Task ListObjects_Test6(MinioClient minio)
        {
        

         
                              v
        r st        ar        T
        ime
         
        =
         
                                       v
        ar bucketNa
        domNam        e(        1
         a        Prefi
         = GetRa
        n
        do        mN        a0                     var nu
        m
        Obje        ct
        s
         = 1015;
  
        vr        s = new 
        iction
        ry<st
        ing,
        string>
        {
        

                   
        { "bu
        ck        m
        ae         },
    
         
             {         "o
        b
        jec
        tName", objec        te        x },
  
                             
         
         
        cur
        iv        e", "fal        se" }
           
         
        };        
                             ao        jec
        me        sSet = n
        w
        Ha        h
        Ss        n>        ;
              
         
        try
  
                     {
 
                   a        e
        t(        m         cketNa        e).
        o
        n
        l
        e)        
                
           var tasks        = ne         Ta
        k
        [
         
                         (        i
         
        umO
        cts; i++)
         
         
                                {
        

              
         
         
         
         
        bj 
        e
                         
         
        tasks
        [
         - 1] = Pu
        t
        O
        bjecs        k(m
        i
        ni        e,        o
        j, nul        ,         ull
         0,         u
        ll,
      
         
         
        Genr        at        eSt
        e
        m
        r
        o
        S
        ed
        1));
     
         
         
          
         
        e
         se
        ver wit        h         o
        current r        quests        
 
         
         
         
        % 25 =        )
        

         
                                           
         a        ait Task
        .
        ay        2000
        )
        Conf
        i
        ureA        w
        a
        f
           
         
            await Task.WhenAll
        (
        t
        as        s
        alse);
    
         
           
         
           v

             v
        istA
        gs
        = ne         
        O
        ject
        Args()
         
        bu
        k
        e
        ex        ctNamePrefi
        x
        )
   
         
         
        t
        )
                
                             W        s(fa        se);
                                
         
        v
        nio.L
        st        sy        c(        li        tA        rgs        )
        observ        ab        le        .S        bs        ribe(
        

         
                               
        i
        tem        =>
        

         
        {
   
                                                   
         
         Ass
        e
        r
        t.IsT        ue        (i        em.Ke        yS        jec
        NamePrefix
        )
        );        
                                                                  if
         (!obje
                       
                        
              
            
              
        ew
        Min        tL        gge
        ("
        ist
        Objects_Te        st        6",
         
        list
        O
           
         
                              "
        est        s         wh        e
        t
        her
         
        L
        i
        i
         obj        cts 
        or
        r
        e
           
                  
         
                                   .FAIL, Da        eTime
        .
                                   "Failed to add. Object
        ready e        m.K        ey, 
        "
        ",         ar
        g
         
           
         }

                         
         
          
         
        count++;        

         
         
         
            
         
            
         
         
        },
         
         
                  ex =>
         
         
        rt.Ar        qual(        oun
        t
        ,         umO
        b
        ect        ));
                     
             aa        t Ta
        s
        k.Del
        a
        y3500).Confi        Aa        alse);
 
              
           ne
         Min
        Logger("ListObjec
        t
        s_Test6", l
        stObj
        ec        gnature,
    
                 
         
        Tests wh
        e
        the
        r ListObjects
        lists more
        t
        an 1000 objec
        t
        s 
        c
        orrectly(max-
        eys = 1000)", Te
        t
        tatus.PASS,
 
         
          
         
                   Da
        eTime.Now 
         
        tart
        Time, args: a
        gs).
        o
        ();
                }

         
              
         
        atch (
        Exception ex         
                  ne
        w
        MintLogger
        "
        Li        s
        6", listObje
        c
        sSignature,
    
         
                  t
         whether Li
        s
        Objects
        lists more 
        than 1000 obj
        cts correctly(
        a
        -ke
        s = 100
        0
        )", Te
        s
        t
        S
        tatus.FAIL,
                    .Now 
         startTime
        ,
         ex.M
        e
        sage, ex.T
        o
        S
        tring(), args:
         
        args)
        .
        Lo           
         thro
        ;
           
            
        }
        
        f
        i
        na        {
 
         
           
         
         
         
        a
        a
        t 
        earDown(mi
        n
        o
        , 
        bu        nf        );

           
         
         }
    }

        internal static async Task ListObjectVersions_Test1(MinioClient minio)
        {
  
         
                   
        v
        r s
        t
        rtTim        =
         
        ate        Ti
        m
        ame = GetR        nd        mN        me        (1        );                    
         
         
         
        ;          ob        ect
        ame 
         pr        fix + G
        tRandomN
        v
        r 
        rg
         
        =
        tring
        
                       {
        

                                   {         "        ucketN        ae        "
             
            
         
        { "objec        tN        am        e", b        me }
        ,
        

        ex        
                                  
        rec
        ve        , "fa        se" 
        }
        ,
                    {         ,        " }

         
               };
                        v
        ei        r
         
              try
  
         
             
        {
        

        iu        _Te
        s
        t(        inio, bucketNa
        m
        e).Confi
        ga                va
        r
         tasks = 
        n
                   fo
         (
        a
                                         
         
         {
   
         
            
         
           
         
           tasks[t
        a
        skIdx+        ] = PutObje        ct        _
        Ta        , o        j
        e
        ctNa
         
        amF        omSeed(1))        

                                       task
        s[
        kIdx+        +
        Na        e, o
        ject        Na        me 
        ,         nl        u0               
                    r        e
           }

    
         
            
         
         await Ta        s.                 await ListObjc        ts_Te
        t(mini
        o
         bucketName, pr        ef        ix, 2,         
        u
           aw
        ai
        t
        e
        A
          
          
        w 
        L
        istObjec
        t
        sArgs        ()        

                                   
         
         
         
        cketN
        me)        
          
         
                               
         
            
         
        .
        WithRecursive(
        t
        rue)

         
         
        Wit        ersi        ns(tru
        e
        );
                            var c
        ount          0;
                    var
         
        ;

  
                                         v
        r observable        = min
        o.Lis        Ob        ec
        sAsy
        c(list        Ob        jc        ts        Args);
 
         
           
                     
        v
        r su
        b
        script        on        = obs
        e
        rvab
        l
                       i
        t
        em 
                         
         
          {

         
                    
         
                             
         
                  I
        .Sta
        ts        With(pref
        x)
        );
        
        unt        ++        
                                   objectVer        i
        ons.Add(new         Tu        le<string,
         
        tem.Ve        ionId))
        
                                      },

                         
            
        x =>        thr
        w ex,
   
                             
                    
        (
         =>         As        s
        ert.Ar        eE        qu        al        (coun
        t
        , nu
        mO        

      
         
           
        ait Task.D        el        y(
        4
        00        0)        Conf
        i
        ur
        e
        Aw        ai        t(        false
        )
        ;
        

            
         
           n
        ew         M        int
        L
        o
        g
        tVers        o
        itObjectsSig        na        ture,
         
        er        Li        t
        ects wi
        t
        h v        er        io        n         lists all 
        o
        b
        jects along wi
        t
        h all
         
        v
        or        ho        t matchi
        g a pr
        fix n
        n-re
        ursive",
               
         
        TestStatus.
        ASS, 
        Da        me.Now - star
        Time, arg
        :
        args).Lo
        g
        ();
        
        }
  
             catch
        (
        xception ex)

         
          
         
            {
       
            ne
         
        intLogg
        er("ListObjec
        Versions_T
        s
        1", li
        t
        bjectsSignatu
        r
        e,
        

                     
          "T
        s
        s w
        ether List
        O
        bjects
         
        ith ve
        rsions lists         
        long with al
        l
        version id
         
        fo        t
        matching a p
        r
        fix non-re
        u
        rs         
                
        T
        stStat
        s
        .F        e
        Now - start
        T
        me, ex.
        e
        ss        r
        ng(), args
        :
        args).
        og();
     
               throw;
                }
    
         
         fi
        ally
        

             
         
          {
  
         
              
         
         
        a
        w
        ait TearDown(minio, buck        igure
        wait(false);
      
         
         }
  
         
        }

        internal static async Task ListObjects_Test(MinioClient minio, string bucketName, string prefix, int numObjects,
            bool recursive = true, bool versions = false, Dictionary<string, string> headers = null)
        {
         
                      
        vaa        Now;
        

                      var 
        n
         = 0
        ;
            
         
         
         
        ar a
        r
        Arg
        s
        ()
          e
             
          .W
        i
        thPrefi
        x
        (pref
        i
        x
        )
            
        .
        WithH
        e
        ae             
              .WithRecu        s
        i
        ve(re        cu        ive)
     
         
             .
        W
        t
        h
        i
                     i        fn        s)        

        .i        st        ObjectsAs        yn        c(arg
        s
        )
        on = o        se
        r
                          item 
        =>        
            
         
        
           
         
                               
         i        sNu
        lO        rEmp
        y
        p
        re           
         
        sT        u
        (item.Key.
        t
        rtsW        t
        h
        (prefix));                      
         
                    co
        u
        n
                   
        ,
                            
         
         ex         =>         thro        w
        =>         {         }
         
         var o
        b
        sb        e
         = m        nio.Li
        s
        tObje        t
        sA        s
        y
        var
         s
        ub        le.Subscribe        (
 
         
           
         
           
             
         
        item =
        >
          {
        

                 
         
         
        r
        u
        h
        pr
        fix)        ;
          
         
        nt        ;

                       
         
               }
        ,
        
                       
                        ex => thro
        w
         
        e
          (
         => 
        {
         });

                               
         
        }
        

                       a        wa        it Ta
        s
        k.Dela        y(        1
        wa        it
        false        );        
                     AsserA        reEqual(numObjects,
         count);
    }

        #endregion

        #region Presigned Get Object

        internal static async Task PresignedGetObject_Test1(MinioClient minio)
        {
        ime = Date
        Ti        e.N
        o;        
       
         
        var
        b
        cketN        me =
         
        etRa        do        Name
        (
        1
        5);        
          
         
            var obj
        GetRan        om        bjectNam
        (1
        0
        r
        10
               v        ar
         
        downloadFil         = "downloa        Fi        e
        Name";        
               var args         =         
        n
        , s        ri        g
        
              
        {
                           
        buc
        etN        ame", 
        ucket
        ame
        },
    
            
        { "o
        jec        tN        me", ob
        ec
        e }
        
   
                       { "expir
        s
        nt        ",        exp
        res
        I
        nt.        oS        tring
        ()
                  try
                      
         {
 
         
                         wai
         
        etup_T        es        (m
        i
        io
        ,         uc        ketNa
        m
        ).
        C
        onfigu        eA
        w
        a
        i
        (fals        e)        
   
         
                    
         
         
         
        estre
        am        n
        Fro        Seed
         
                               
        var putO
        b
        jectA
        r
        s = new Pu
        t
        O
        bjectArgs(        
   
         
             
         
         
        i
        tb        tName)
 
              
             
            
        .WithObject(obje
        c
        tName)
    
             
         
              
         .WithStre
        a
        Data(f
        lestre
        a
        )
 
                  
                .WithO
        jectSize(
        i
        estr
        e
        m.Le
        gth);

 
         
             
         
             await
         
        minio.
        P
        tObjec
        t
        sync(pu
        O
        ject
        Ar        C
        it(
        alse);
  
         
               }
        

        
  
                var s
        Ob        c
        Ar        g
        ec
        Ar        g(                               
                  t
        (bucketNa
         
                  .W        ithObjec        t(        o
        bject        Na        me        )
        va        r stat        s         = awa
        i
        t minio
        .
        c
        (statObject        Ar        gs
        )
        .Configur
        e
                                       e        rgs 
        =
         new Presign
        d
        G
        etObject
        A     .With
        Bucket(bucketNa
        m
        e)
                             .W
        t
        Objec        tt                       
        va
         
        p
        resign
        e
        d_url        = await        mi        io        .Pres
        i
        g
        figur        A
        wait
        (
        fal
        se        ;
                                  
          awai
        t
         
        D
        o,         pr        e
        d
        ur
        Aw
        it(fa
        se
        );         
        w
        it
        e
        I
        own
        oa        dF        ile);
            
             
         
        var         f        ile_read        _siz        e         .Len        th        ;
         
         
        // Compare
         
        the size 
        o
        ing 
        he         e         
        lu        en         t        e a        tua
        l
         
        o
        on
         t
        h
         A        s
        re
        d_
        ize,         s
        se                                       n
        e
        w
        "esignedGetObje
        t_T        es        t
        1
        ", pr
        e
        sG        bject        ignat
        u
        re,
            
         
           "Test
        s
         whether P
        re        igned
        G
        et        ct         retrieves ob         from bucket", TestStatus.PASS                
             D
        teTim
        .Now
        - startTime, args: args)
        .
        Log();
    
           }

                    catch (Exce
        tion ex)

         
             {
 
         
           
               new Mi
        tLogger("P
        e
        ignedGetObjec
        t
        _T
        e
        st1", presign
        dGetObject
        i
        nature,
           
         
          
         
         "Tests wheth
        r Presigne
        G
        tObj
        ect url retri
        ves object f
        o
         bucket", TestStat
        u.FAIL,
     
            
         
           
        ateTime.No
        w
         - sta
        r
        Time, 
        ex.Message,         )
         args: args)
        .
        og();
    
         
                   
             }
     
         
         finally
 
         
                   
           File.Dele
        t
        (downloadF
        i
        le);
   
         
         
              await
         TearDown(minio, bucketN        reAwa
        t(false);

         
             
         
        }
    }

        internal static async Task PresignedGetObject_Test2(MinioClient minio)
        
        sta
        tTime         =         DateT        im        .
        o
        ;
 
                              v        ar bucket
        N
        a
        5
         
         
             var o
        b
        t
        Name(10);

         
                               var e
        x
         
        args = ne        w         icti
        o
        na        y<strin        g,        s
         
             {        "bucketN
        am        ",        bucke        tN        m
        e
         },           
         
         
         obje
        ame }
        ,
                                            "ex
        p
        ire        In        ", ex        pire
        s
        I
        nT        oS
        tr        ng()
         
        }
         
        
  
                                wait        p_T        st(m
        i
        n
        n
        figureAwait        (
        false);

        i
        ng (var fi
        l
        es        tream = r
        s.        Gen
        rateS
        r
        amFr        om        S
        ed(1 *         B                   
             {
                           
             var         pO        tOb
        ectArgs)        
            
                                             .WithBuc        ket(
        b
        uc         
                           
         
        .WithObje        ct        (
        ob         
                                  
         
        .WithStrea
        m
          
                      .Wi        th        Objec
        S
        ze(fi
        estre
        a
        m.Len        gt        h          await m        inio.        Pu        tObj        ec
        t
        A
        rgs).        C
        ir        )
        ;
                     
                                
         
        var        sta        t
        Ob        ew t        at        bje        tA        g                     WithBuc
        k
        e
           
                  .Wit
        O
        ject(object
        N
        ame);

         

        stats = aw
         mi
        io.Sa        ts        ync
        at
        bjectArgs
           
         
         va
         pr
        Args =         
         g        edG        tO        btArgs()
             .W        thB        cke
        t
        bc        k
        e
        tNam
        e
        )
                             t        hObject(objectName        
                                                .WithExpiry(0);
                          
         
        = awai        t         inio.Pr
        si        gnedGetOb        je        ctAsync(
        reA
        gs).Confi
        ureAwa
        t(fa
        se);
 
                   throw ne
        Ex        eption        

                     
                 
        "
        resi
        g
        edGe
        tO        bj        ect
        A
        s
        y
        dto throw an In
        a
        lidEx        iryR
        ng
        e
        )
           
        cat        h (Inva
        l
        idE        pi        yRangeExceptio        n)        
                       
         {
                   new Mint        og        er("Pr
        e
        ", presignedG        t
        bjectSign        tu        e,
             
           
                      Tes
        s whet
        er        Pr        ignedG
        etObject url         r
        e
        trie
        v
        when in
        v
        ali        d         x
        iry         is        set.
        "
          
         
               
         
          
         
         TestSta
        t
        us        ASS,
         
        ateT
        im        e.        ow
         
        -
         
        : arg
        s
         
             ca
        d
        tion
         
        ex)
                    
           {
                         
         
         
        "Presi        nedGetOb
        j
        ect_T
        e
        t2", presi
        g
        n
        edGetObjectSi        n
        a
        ture,
        

         
                  t         her Pres
        gnedGe
        Objec
         url
        retrieves object from bu
        c
        ket when in
        alid 
        ex         is set.",
  
                 
         
         TestSta
        t
        us.
        FAIL, DateTim
        .Now - sta
        t
        ime, ex.Messa
        g
        e,
         
        ex.ToString()
         args: arg
        )
        Log();
            
        t
        hr
        o
        w;
        }

               cat
        h
        (
        Exception ex)
            
         
         {

                  
         
        new Mi
        n
        Logger
        ("PresignedG        t
        ", presigned
        G
        tObjectSig
        a
        tu         
              "Tests
         
        hether Pre
        i
        gn        u
        l retrieves 
        o
        ject from 
        b
        ucket wh
        e
        n
        invalid exp
        iry is set.",
                  atus.
        AIL, DateT
        i
        me.No
        w
        - startTim
        e
        ,
         ex.Message, e
        x
        .ToSt
        r
        in        rgs).
        o
        g()
        
         
         
        thr
        o
        w;
        }
        f
        i
        n
        l
        y

         
                            own
        minio, bucket
        a
        e).
        onfigureAwait
        (
        fa        }

        internal static async Task PresignedGetObject_Test3(MinioClient minio)
        {
               va         s
        t
        art        ime =         Da        t
        e
        Time.N
        o
        w
        etNam
         = Ge
        t
        RandomNam        e(        1;           
         
         
         v        ar objectN
        a
        me = 
        Ge        tR        m
        ar 
        xpire        sInt = 100
        ;
           
            var reqDa
        t
        e         w
        .AddSeco        nd        s
        d
        ownl        ad        ile 
        =
         "downl        oa        dFi
        l
        e
             
         
        va         rgs        = 
        n
        ew Dictionary<s        rin        , string>
        Nam        , b        cket
        a
        ,
            { "objec
        t
        Na        }
        ,
                           { "e        xp        iresI        nt        ",        r
        ing() },
 
         
                  
        {
        "
        reqParams"
        ,
        

         
         
        o
        se-co
        tent-
        t
        ype        application/json,res
        po        se-c        nt
        e
        n
        t-disp        sition:        t
        t
        ach        en
        t
        ;
        oc u 
                   son ;"
  
         
                   { "r        eq        ate",         reqDate        .T        oS        ti         
        
 
             
        };
                               try
               {        
          
        st(mi
        i
        o, bu        ck        etName).ConfigureAwait
        (
                  g (
        ar filestr
        e
        am =         G        nerateStreamFrom        eed(1 * KB))
 
         
                      
         var         ut
        bjectArgs =         new Put
        bje
        tA        rg        s()
                       
                               .W
        thB        cke
        (b
        cke
        tN        a
         .WithOb        je        ct
        (
        obje
        c
        e)
             
         
           
         
        h
        trea        m
                              .Wi
        bj
        e
        e
                await mi        ni        o.PutO        bj        ct
        Async(put        Ob        je        ctArgs).Configur        eA        wait
        (
                }

                   var s        atObje        Args        = new Sta
        Object
        rgs(
        
                       
            
                   Withu        cket(b
        ck
        tNa
        m
        e)
        thObject        (o        bj
        ec        tNam
        e
        ;
               
           
        v
        r st        ts        = a
        w
        it
         
        minio.        St        a
        t
        ec
        t
        Async(st
        a
        tO        ectA
        r
        s).C        nf        gur
        e
        A
        w
                                        
          var reqPaams = new Dic        io        ar        y<string,
        st
        r
         
           
          [        "r        sponse-        c
        ontent-type"] = "applicat        on/json",
                               [        re        spon
        s
        ion"] 
        ttachm
        nt;f        le        name=  MyDoc 
         m         
           nt.jso         ;"            
                      }        ;
   
                                       var pre
        rgs         = n
        w 
        res
        i
        gne        
         
                     
         
        WithBu        ck        et
        (
        buc        ke        N
        me)
                                   
         
           .Wit
        h
        bj        ec        t
        (obje        ct        Nam
        e
        )
        

            
         
            
         
                   .        W
        i
        t
                               
              .WithHeaders(req        Pa        ra        s)         
                  stDat
        (reqDate)        
    
         
              v        r         r
        e
        si        ned_url = awai        t         minio
        .
        PresignedGet        cA        (preArgs
        .Confi
        ureAw
        it(f
        lse);

            using
         
        var respons
         = aw
        ai        nio.WrapperGe
        Async(pre
        i
        ned_url)
        .
        Con
        figureAwait(f
        lse);
    
         
             if (resp
        o
        ns
        e
        .StatusCode !
         HttpStatu
        C
        de.OK || string.IsN
        u
        ll
        O
        rEmpty(Conver
        .ToString(
        e
        pons
        e.Content)))

               
         
         {
     
         
              
         
           throw n
        e
        w
         A
        r
        gumentNullExc
        ption(nameof
        r
        sponse.Content), "
        Unable to dow
        load
        v
        a p
        esigned UR
        L
        ");
  
         
              
          }

               I
        True(respons
        e
        Content.He
        d
        er        "
        ontent-Type"
        )
                  
         
                  r
        qParams["res
        p
        nse-conten
        t
        -type"])
        )
        ;
         
                  er        e.Content.H
        ea        Content-Disposition")
                .Contains(reqParams["response-content-disposition"]));
            Assert.IsTru        o
        nt        G
        tValues("
        C
        ntent-L
        e
        ngth").C
        o
        n
        ains(stats.
        Size.ToString()));

            g (va
         fs = new 
        F
        ileSt
        r
        am(downloa
        d
        F
        ile, FileMode.
        C
        reate
        N
        ew           {

         
           
                  
        w
        it 
        r
        esponse.Content.CopyTo
        A
        s
        n
        (f
        s
        ).        it           
         }

         
         
        var
        writtenInfo =
         
        ne        i
        le);
     
         
              var 
        fi        e
        nInfo.Leng
        t
        h;

      
                  i
        ze of the file
         
        downloaded
         w         
                 // pr
        e
        signed_url
         
        (expec
        t
        e)        objec
         size
         
        on the server

         
                   As
        s
        e
        rt.AreEqual(fi
        l
        e_rea
        d
        _s        ie           
        ew MintLogger(
        P
        esi
        nedGetObject_T
        e
        st        O
        bjectSigna
        t
        ure,
     
                  w
        hether Pre
        s
        ignedGetOb
        j
        ec        ves
        objec
         
        rom b
        cket 
        w
        hen override re
        s
        ponse headers 
        s
        e
        nt",
         
         
             
         
        Te        S, 
        ateTime.N
        w
        - s
        artTime, a
        r
        gs: ar
        g
        ).Log(
        );                  p
        tion ex)
        {
    
         
         
            new MintLogger
        ("        c
        t_Test3", presignedGetObjectSi
        g
        a
        ure,
                "Tests whether PresignedGetO        t
        ri        fro
         bucket
        w
        en 
        verride response heade
        r
        s          
              Test
        S
        tatus.FAIL
        ,         t
        artTime, e
        x
        .Message, 
        ex        s
        : args).Lo
        g
        ();

                  

                }
 
         
              fin
        al         
                File.De
        l
        ete(dow
        n
        lo           
             await Te
        r
        own(m
        nio, 
        b
        ucketName).ConfigureAwa
        i
        t(false
        )
        ;
        
        }
   
         
        }

        #endregion

        #region Presigned Put Object

        internal static async Task PresignedPutObject_Test1(MinioClient minio)
        {
                    
         
         v
        r 
        tartTi
        m
        e =         Da        teT        im        e.Now;
        

                       v
        ar bucke
        tN        me        = Get        R
        a
        ndomNam
        e
        (
        1
        o
        Objec
        Na        me        (1        );        
                var exp        resIn
        t
         = 1000        
               
        v
        ar file
        N
        a
        e         = Crea
        eF
        le(1         * KB         d        t
        Fi        e10KB);
        
                       
        Dc        ng, st
        r
        in        >
  
         
             {
                          
         
        { "        uc        ket
        N
        ame", buc
        k
        et        ame },
        

            
                   
         
        objectNa
        m
        e         ,
     
         
                      {        "ex
        p
        iresInt        ",         
        expi
        resI        nt        .
              
        }
        ;
    
         
           try
 
         
              {
        

                        
         
           await 
        S
        etup_Test(minio, buck
        et        w
        ait(fals
        e
        );
      
         
             // Upload with presigned 
        u
        r
        l
        
         ar pre
        s
        ignedP
        u
        tObjectA
        r
        gs         = new
         
        Presign
        e
        dP        tObject
        A
        rg        s(        
    
         
                                
           .W        it        hB        cket
        (
        bucke
        tN        ame)
        

                
         
         
         
         
                  (obje
        t
        Nam        
 
         
                                   .        Wi        hE
        x
        piry(100        0)        ;
           
         
               v
        ar         p        resign        ed        _u
        r
        l         o
        nc        prs        ig        nedPutO
        b
        jectArg
        s
        ).Confi        gu        re        wa        t
        (f        al        s
        e
        );
           
         
        await
         
        U
        n
        ed_u        l, fileName
        .
        fig
        reAwait(
        f
        alse);
              
         
         
        s fr         object from s
        r
        er
                 
         
          var         s        t
        a

        ew StatObjectArgs(        )

          
           
            
         .WithBucke        (buc
        etN
        me)
     

        thObje        ct        (objectNam
        

                                  
         
        var
        sta
        s = aw
        it min        .Sta
        je
        tAs
        nc(sta
        nfi        gu        re        Aw
        a
        it(false
        )
        ;
                                            // C
        o
        pare 
        w
        ith 
        f
        i
        oa
        
         
                  va         wri        tenInfo = new F        il        eIn
        fo(fileName);
                    va        r         fil
        e
        ittenInfo        .Lengt
        ;
                            Ass        er        t.A
        eEq
        al(file_wr        tten_
         st
        ts        Size;        
            
               n
        w M        in        tLogg
        r("Presg        ne        dP        ut
        Ob
        dPutObject
        S
        igna
        t
        ,
     
         
                             
                  Tests w
        h
        th        er         P
        r
        sig        ne        P
        u
        tOb
        je        c
        t url uploads object to b
        c
        ket",         Test
        ta
        tus.PASS,
 
        ate
        ime.Now - s        t
        artTime, args: args        ).Log()
        ;
        }
        catc        h         Ex        ept
        io                    new MintLogger("PresignedPutOb        ject_T
        st1", 
        resi        edPutOb        ec        Si
        ture,
 
                
             "        Te        s
        s wh
        et
        ject         ur        l upl
        o
        ads o        b
        ect to b
        u
        cke
        "
         Te        stStatu
        s
        FA        IL        ,
        
      
                    
         
            Date
        T
        i
        m
        .Now         -
        star
        t
        T
        ime        ,         e
        x
        .e        Strin
        g), args: a
        
                    throw
            
         
         fi        na        lly
        

                                {
    
         
         
        arDow
        (mi        io         bu
        c
        ke        Nam
        e.        Co        nfigureAw
        a
        i
        t(false);
                      
         
             i        f         (IsMintEnv(
        .De        (fileName);
            }
    }

        internal static async Task PresignedPutObject_Test2(MinioClient minio)
        {
   
            v
        ar        r
        eT        m
        ow;
                      
         
        ar bucke
        t
        Nam
        e GetRandomN        am
        (15);
             
         
        var o        bj        ectName         =         Ge        td        Ot        e(10);            
         
          
         
        var expiresIn
         = 0;

                      
        var 
        a
        Di        ct
        onary        <s        tr        n
        ,
        strin        >
           
         
          
        {
          
         
                       { "bu
        c
        k
        ame },
   
         
              
         
        { "        ob        jec
        t
        c
         
            { "e        xp        ire
        sInt",         ex        piresI
        t
        .
         };
                        
        r
        y
         
                              await S
        etup        _Test        (minio
        ,
        re        A
        ;
 
        u
        eam =r        sg        .Generate
        S
        tream
        F
        omSee        d(1 * 
        K
        B
        ))
                    
        {
        
                       
         

        ObjectArgs = new PutObject        Ar        gs        (
           
                    .W        ithBuck        et        (bucketNa        me        )

         
           
                                   .WithObjec        t(        obj
        e
        c
         
              .With        St        reamDa        ta(fi
        l
         
            .WithO
        b
        jectS        ze        (fil
        e
         
                                    
         
         awa
        i
        t
        tA        sy
        c(putObjectA        rg        s
        .
        onfig
        reAwaa        se
        )
        ;
                    }        

                         
         
          var statObjec        t
        Args         =         n
        e
        s()
 
                             .With
        B
        cket(buck        ta        me)
               
                                
        .
        W
        )
        
 
              
           v
        r         ta        s 
        tat
        bje        cs        at
        bjectArgs).Con
        f
        i
         
                  
        v
        ar p        esig        ed
        P
        w Presig
        n
        edPutObje
                      .W        t
        B
        cke        (b
        cketN
        a
        me)                                  
         
           i        jectName)
                    
         
                                          (0);
                                          va         pr        ei        ne
        nio
        PresignedPu
        O
        jec
        Async(p        re
        s
        ignedPut
        O
        bj        fig
        reAw        at                 
         
        new Mi        tL        gnedPu
        t
        Ob        ect_        es
        t
        2", presignedu        bje        tS
        i
        gnat
        u
                   
         "Tests w        e
        t
        her Presigned        Pue        ct url retr        ie        ves o        bj        c
        nvalid ex        pi        y is
        se        t.        ,
                                                T
        stS
        atus.PA
        S, Dat
        Ti
        e.        ow         - 
        startTim        , a        rgs
        :
         arg
        s
                     }

         
           
         
         ca        tch (In
        v
        lidE
        x
        iryR
        a
        n
        geE
        x
        ce        ption)
        {
        
         
          new M        int
        og
        ger("Presign        Tes
        2", presig
        n
        edPutObjectSignature,
           
                           "Tests whethe         Pre
        s
        retri        v
        s o        bjc         buc
        et 
        hen         in        val
        d e        pir
         i
         s        et        .",
                                     T        stSt
        a
        ow -         st        ar        tT        m
        e, 
        rg        : arg        s)        .L        og(
        )
        
                    
            }
 
         
          
         
          catch         (
        E
        x
        c
        ptio
        n
        e
           ne
        ignedPubject_Test
        bjectS        g
        natur
        e
        
                                    
         
           "Tests wh        et        he        r
         
        Presi
        g
        n
         r
        t
        r
        ieves obj
        e         f
        rom bu
        c
        ket when
         
        invalid expi        ss        ,
      
              
          Tes
        Stat
        s.FAIL, DateTime.Now - s
        t
        artTime, ex
        Messa
        ge        .ToString(), 
        rgs: args
        .
        og();
  
         
           
              throw;

               }
 
         
            finally
 
         
          
         
           {
        
           await T
        a
        Down(minio, bucketN
        a
        me
        )
        .ConfigureAwa
        t(false);

         
         
            }
    }

        #endregion

        #region List Incomplete Upload

        internal static async Task ListIncompleteUpload_Test1(MinioClient minio)
    {
         
        var star        tTime = DateTime        No            w          
        v
        me         
        );
  
                     var ob
        j
        ectNam        e         Getn        O
        0            
        a
        r c
        o
        ntentType         =         "gzip        ";              
         
         
        a         ar        gs         
        =
        y
              {
  
         
           
           { "bucketN
        a
        m
         
               { "
        r
        ecursive",
         
         
             try
 
         
                              {
        a
        it Setup_Test(
        m
        ini        o
        i
        gureAw        ai        t(false)
        ;
        
            
        llat        io        nkenSource();
          
         
                cts.Ca        ce        After(        imeSpan
        .
        FromMi        ll        is        

           
                {
             
         
                             
         
        u
        e
        am =         rs        .Gen
        e
        rateStream
        F
                            v
        e          
        ilest
        engt        ;

       
         
                       var put
        O
        b
        ject        Ar        g
        s(        )

                                    h
        ucket(b        uc        ketName)
          
                    .W        thOb
        j
        ec        (objec        tN        am
        e
         
          .WithStr
        e
        amData(fil
        e
         
                .W
        i
        t
        h
        Ob        le
        tream.Let                                                  With        onten
        t
        Type        (c        ontentTyp        e)        ;
       
         
         
                       wait mi
        n
        io.Pu
        t
        O
        bje
        tArgs, cts
        .
        Token).Config        ur        eAwait(fals        e)        ;
        
            }
                            cat        c
        ledExceptio        n)        
  
                 {
       
           
            var l
        stArg          ListI
        comple
        eUp        oad
        s
        Arg                              .Wit
        h
        Buc        e
        t
        buck        tNa        e
        )
        ;
 
                           
         
        var o        bs        rvab        lm        c
        s(lis
        A
        rgs        );        
                                                       var         su        bscript
        ion = obser
        (
 
                                   
         
             item => Assert.IsTr        e
        (item.Key.        Co        ntains(objectName)),
        

          ex => Assert.Fail        (;               }         
              
           c
        tch         Ex
        tio
         ex)
          
                      {
           
         
                   ogger("        Ln        le
        e
        pload_Tes        t
        1
        lis
        t
        ncom
        p
        l
        ete
        Up        l
        e
             
                   "        Te        st         whet        he         t        d s        s", TestStatus.FAILt        Now - sta        rtTime        ,
                    ex
        String        ()        )o                                           
          re        tur
        ;
           
                                
        

  
               
        ew Min
        Lo
        ger
        (
        "L
        _Tes        1", l        s
        t
        Inco
        m
        leteUplo
        a
        dsS
        g
        at        ur        e,
    
         
                                 "T
        e
        ts
         
        whether 
        L
        i
        s
        com
        p
        eteUp        oa         p
        a
        s
        s
        PASS,
         ateTime.No
        me).Log
        

        (Exce
        ti        n         x)
 
         
             
         
        
                                    ne        w
         MintLogger("        i
        s
        tInco
        m
        p
        es         li        completeUplo        ignature,
                "Tests        ther Lis
        Incomp
        eteUp
        oad 
        asses", TestStatus.FAIL, D
        a
        teTime.Now 
         star
        tT         ex.Message,

                 
         
           ex.To
        S
        tri
        ng()).Log();

                  
        t
        row;
        
        }
        
 
         
              finally
                {

         
                 await Tear
        D
        ow
        n
        (minio, bucke
        Name).Confi
        u
        eAwait
        (false);
    
           }
         
          }

        internal static async Task ListIncompleteUpload_Test2(MinioClient minio)
        
        art
        TN                    
         var bucke
        tN        am        e = m        (1        5);
     
         
          var
         p        r
        prefix/";
        
        v
        a
        r
        ref
        i
        x + GetRand
        o
        mName(10)        ;
                var cont
        e
        nt
        T
        y
        p
        v
        a<        ng        >

         
           
         
         {
                            {         "c        te         
         
                      refix
        "
           
        { "r        ec        ur        sive",          }
        

                   
                      {
  
         
                 a
        w
        b
        ucketNa        me        ).C
        o
        nfigureAwa        i
         
                                   using
         
        var         c
        e
        nS        ou        rc        e(        );
       
         
            c        s.Can
        c
        elAfte
        r(        c
        onds(15));
                      
         
             try
  
         
         
             
        usi        ng         v
        a
        r f        il        er        eam          rsg.
        G
        nera        te        Strea
        m
        F
        romSeed(50 * M
        B
        );
  
         
                  r         ize f        estream.Leng
        t
         
        rgs
        =         ew Put
        b
        ect
        rgs()
                                                  .Wi
        t
        hB         
                                                        .WithObje        ct        (
           
                 .
        i
        hStre
        a
        mData(ft        a
        m
        )
                       
         
                  ect
        ize(filest        rea
        .
        ength          
        tent
        yp
        (conte
        n
        tType);        

            
         
           
         
               a
        w
        ait minio.
        P
        u
        te        ec
        Ar
        s, cts
        .
        Toke
        n
        )
        .
        Co        (
                    }
         
                                c
        a
        C
           
               {
 
         
                              ar        listArgs =         ne         Lis        tI        compl        et        eU        ploa        dsAs         
         .WithBucket(bucketName)
                  
         
        .WithPrefi
        x
        ("mi
        n
        oprefix"
        )
        
  
         
                 
                  (fl        se);
 
         
         
         
         
           
         
         
         
        e = mi        i
        s)
        

                                             a        ubscri
        ption =         o        bserv        ab        e.Subscri        be        (
                         
         
        ert        .A        eE
        l(item.e        y,         o        bj        ctNa        me        ,
       
                                                 Ass
        er        .Fail())
        ;
        
            
         
        }
        

 
        M
        intLo        ger(
        Li
        s
        istI        completeUp
        l
        oads        Si        gnatur        e,        
             
            "Tests whether ListIncompleteUp         qualified by prefi        ", Test        tatus.        ASS
             
                  DateTime.Now         -         start
        T
        ime
        gs: a        rgs).
        L
        g(
        )
        ;
     
         
        xc
        e
        pti        on         ex)        
 
         
         
         
         
           
        {
        

         
        Mi        tLo
        g
        n
        ad_Test2        c
        ignat
        re,
                                         
             
         
        "Tests        whet
        h
        e
        r List        nc        mpleteU        p
        load 
        p
        a
        q
        db        efix", T
        stStat
        s.FAI
        ,
  
                     DateTime.Now 
        -
         startTime,
        ex.Me
        ss         ex.ToString(
        , args: a
        g
        ).Log();
        

           
                 thro
        ;
        
        

               finall
        y
        
 
         
              {
     
              
        w
        it TearDown(mi
        nio, bucketNa
        e).Configu
        e
        wait(f
        l
        e);
        }
        

          
         
         }

        internal static async Task ListIncompleteUpload_Test3(MinioClient minio)
    {
 
         
             var startTime = Dat
        T
        i
         
         var buc        k
        m
        ame(15);
  
         
            var
        pfix = "min
        ioprefix";
  
        o
        fi        x+        +         GetRandom
        N
        a
        me        (1        0) + "/suff
        ntent
        e =
        "p";
                var ar
        g
        s
         
        y<s
        t
        ring        ,         string>        

         
               {
        

                    {         "bu
        c
        ke
        t
        N
        a
         },
        prefi
         }        ,
         
                  
         
        rec
        ur        si        e", "true" }        
                        };
        

          
         
                   t        r
        y
           
        await Setup_Tes
        (
        inio, buck
        e
        tName        )
        );
          
           
                    si        g va         ct
        s
         =        e
        nSo        ur        ce();
 
         
                  
        c
        n
        .FromMilli
        s
                            try
                         
         
          {
                         
        r
        eam = rsg.G        ner
        a
        teStreamFr        mS        ed(10
        0
                                v
        ar fil        e_        write
        _
        s
        h;        

           
             
                                       var putObje
        c
        tArgs = n        w P        t
        O
        ject        Ar        gs()
                                                    
        .t        u
                             
         
        Wit
        StreamDa
        a
        fil
        stream)
                 
         
         
        z
        e(filestre
        a
        m.Length)

         
        h
        ContentTyp        e(        c
        onte        nt        ype);
 
                   
        u
        tObjectAsync(
        p
        utObj
        e
        c
        r
        A
        ait(
         
         
           
        atch (Op        erati
        n
        anceledExc
        e
        ption)
  
         
        ar 
        l
        istAg        s
         
        = ne
        w
         L
        i
        tIncomplet
        e
        U
          
          
                     .W
        i
        thBu
        c
        k
        et        (
         
        Wi        thPrefix(
        pr        fix        )            .WithRecursiv        e(        true);
      
         
        vable = minio.ListIncompleteUploads(listArgs);

                var 
        s
        bscription
         
        = ob
        (
      
         
           
         
                       i        tem          A
        s
        ert.
        A
        re        Eq        a
        l(        it        e
        e),        
              
          
         
         
        ail());        
                   

      
              new n        istInc        mp
        l
        stIncomplete        Up        lo        a
        sSignature,
                                              t         t
        er
        ListIn
        compl        teUploa         
        p
        asse         
        prefix a
        n
        d rc        ur        ive", Tes
        tS        at
        u
        s.P        ASS,

         
          
         
                
                  D
        teTi
        m
        .N        w          
        s
        tar
        t
        T
        i
        Log()        

        ch (Exc        e
         
                  ne
         MintLog
        g
        er(        "Li
        s
        Incomplet        U
        p
        l
        oad_Test        ",        list
        I
        nc        mp        e
        t
        eUploadsSig
                          
        Tests 
        hethe
         Lis
        IncompleteUpload passes wh
        e
        n qualified
        by pr
        ef        nd recursive"
         TestStat
        s
        FAIL,
  
         
           
                  Dat
        Time.Now -
        s
        artTime, ex.M
        e
        ss
        a
        ge, ex.ToStri
        g(), a
        g
        : args).Log()
        ;
           
        throw;
   
         
          }
  
         
           
        i
        ally
        
        {
        
 
         
         
               aw
        ait TearDown(
        inio, bucke
        N
        me).Co
        nfigureAwait(
        alse
        ;
           
            }
    
        }

        #endregion

        #region Bucket Policy

        /// <summary>
        ///     Set a policy for given bucket
        /// </summary>
        /// <param name="minio"></param>
        /// <returns></returns>
        internal static async Task SetBucketPolicy_Test1(MinioClient minio)
        {
                var
         
        s
        t
        m.        bu        c
        ketName =         Gn           
        e
        0)        ;
 
         
           
         
        var ar        gs         = new Dictionar
        y
        <st        r          {        
 
                  {         "        bN        u
        c
           
         "object        Pr        efix"
         
        bje
        tNam        e.        ubstring
        (
        5
        l
        icyType", 
        "
        re
        it S        tup_Test(        in        o, b        cketNa
        m
                   
         
        using (va        r         f
        i
        lestre
        a
        .G        enerateStre
        a
        mF        romSeed(1 
        *
         
        {
                    
                            var pu        tO        b          P        tO        jec
        t
        Args(
        )
        

        et(b
        c
        ketName)                                                   .
        W
        
                           
         
                   .W        th        St        eamData(fil        es        t
        i
        th        Ob        jectS        iz        e(        fi
        l
                  
                   await
         m        p
        utObjectArgs)
        .
        Conf
        i
        g
        ;
   
         
             
         
        }

            var po
        l
        icyJson 
        =
        
         "{{"        Ve        sion"":""2
        -
        0-7        m
        :Get
        bj
        ct        ""        ,""
        Ef        fe        ct"":""        A
        l
        lowP        i
        p
        a
        sou
        r
        c
        e
        ""        s
        }/f
        o*"",""arn
        :
        aws:s3:::{bu        cN        me}/prefix/*""],""        id"":""""}
        }]          var setPol        cyA        gs = ne        w SetPolicyArg(        )
             
        ucket(
        uc        ke        t
        ame)
   
                                     .W        i
        t
        hPol
        i
                      a        wa        t
         mi
        i.        Se        tPol        ic        yA        s(        Arg
        s
        )
        .igureAwait(
        a
        lse)
        Min        ogger("S        et        Bu
        c
        ketP        ol        cy_Test1", s        et        Bu        cketPolic
        ySignatu        re        , "Tests         w        hether SetB
             
                      stSt        tus.PASS, DateTi
        e.Now 
         st        ar        t
        ime, args:        ar
        s).        Log(
        ;
 
              }
 
                     catch         No        Impl
        e
                       {
        

           
                              new M
        i
        tL
        o
        gg        er        ("S        et        Bu
        c
        et
        P
        olicy_T        s
        t
        1
        "
         s        tB        c
        etPo
        l
        i
        cyS
        i
        gn        a
        hethe
        r
        o
        
             
        T
        eTi        me.
        ow - sta
        rt        ime,
         
        x.Mess        ge         e
        x
        .
        ToS        tring(), arg
        s:        args)        L
         
                   atch (Except        ex)
        {
                 ew MintLogger(        BucketPolicy_Test1", setBucketPolicySi        ure, "Tests whe         SetBucketPolicy passes",
                    TestStatus.FAIL, Da        me.Now -
        startT
        me, e
        .Mes
        age, ex.ToString(), a
        r
        gs: args).L
        g();

                        throw;

               }

         
             fin
        a
        lly
        
        {
  
                 a
        a
        t TearDown(mi
        n
        io
        ,
         bucketName).
        onfigureAw
        i
        (false);
        }

         
          
         
        }

        /// <summary>
        ///     Get a policy for given bucket
        /// </summary>
        /// <param name="minio"></param>
        /// <returns></returns>
        internal static async Task GetBucketPolicy_Test1(MinioClient minio)
        
             
        a
        r s
        ar        tT        ime = D        am        .
        Now;
                va        r bucketN
        a
        m         =
        1
        jec
        Name = GetRan
        o
        Obj
        ctName(10        );        
   
                   r        s 
        =
         n        w         Diction
        ar                   
        t
        Name }
       
         
        };
                           {
     
         
                    
        ame).C        nfigu
        r
        eAwait(false);
        

                            va
        r         po        licyJ        so        n          
        Vr        2-1
        -17        ""        ,""Statm        et        :[""s3
        :GetObj
        ec
        t
        ""
        ]"        "E        f
        e
        ct
        ""
        :
        ""
        Allow"Principal"
        ":{{        "AW        ""
        :[
        "
        "
        *""        ]}},""
        Re
        s
        ou
        rc        e"        ":[
        ""
        a
        rn
        :aws:s3        ::        :o        "","        "a        rn        :a        ws
        :
        s
        3:
        ::{
        b
        uck
        e
        tm        f
        i
        x/        "
        "
        ],
        "
        "S
        id""        :"        """
        }
        }]
        }}
        "
        ;
        
         
         
         
         using  (var filestream = rsg.GenerateStreamFromSeed(1 * KB))
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(filestream)
                    .WithObjectSize(filestream.Length);
                await minio.PutObjectAsync(putObjectArgs).ConfigureAwait(false);
            }

            var setPolicyArgs = new SetPolicyArgs()
                .WithBucket(bucketName)
                .WithPolicy(policyJson);
            var getPolicyArgs = new GetPolicyArgs()
                .WithBucket(bucketName);
            var rmPolicyArgs = new RemovePolicyArgs()
                .WithBucket(bucketName);
            await minio.SetPolicyAsync(setPolicyArgs).ConfigureAwait(false);
            var policy = await minio.GetPolicyAsync(getPolicyArgs).ConfigureAwait(false);
            await minio.RemovePolicyAsync(rmPolicyArgs).ConfigureAwait(false);
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.PASS, DateTime.Now - startTime, args: args).Log();
        }
        catch (NotImplementedException ex)
        {
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.NA, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
        }
        catch (Exception ex)
        {
            new MintLogger("GetBucketPolicy_Test1", getBucketPolicySignature, "Tests whether GetBucketPolicy passes",
                TestStatus.FAIL, DateTime.Now - startTime, ex.Message, ex.ToString(), args: args).Log();
            throw;
        }
        finally
        {
            await TearDown(minio, bucketName).ConfigureAwait(false);
        }
    }

        #endregion

        #region Bucket Lifecycle

        internal static async Task BucketLifecycleAsync_Test1(MinioClient minio)
        {
        
        a
         st
        a
        rtTime = DateTime.Now;
        

         
         
          
         
         v        e         15)
        
        var 
        r
        s =
        new Dictionar
        y
        <s         
          {
      
         
             { "bu
        ck        }
        
        }
        ;
        
        t
        ry         
          await Setup_
        T
        est(minio,
         b        A
        wait(false);
 
         
              }
  
         
             c
        a
        tc        
    
           {

         
                   awa
        i
        t TearDown(mi
        n
        i
        o, bucketName)
        .
        Confi
        g
        ur        )
        new
        MintLogger(na
        e
        f(B
        cketLifecycle
        A
        sy        c
        ketLifecyc
        l
        eSignature
        ,
        "
        Tests whet
        h
        er SetBuck
        e
        tL        c p
        sses", TestSt
        t
        s.F
        IL, DateTime.
        N
        ow        .
        Message,
 
         
                  
         
                  (),
        args: args).
        o
        ();
                    thro
        w
        ;
         
          var rule
        s
         = new Lis
        t
        <L        >();

             
         
         var exp = new
         
        Expiration(Da
        t
        e
        Time.Now.AddYe
        a
        rs(1)
        )
        ;
        com
        areDat
         
         new 
        ateTi
        m
        e(DateTime.Now
        .
        Year, DateTim
        e
        .
        Now.Month, Dat
        e
        Time.
        N
        ow        0);
 
             
         
        var expInDays = (
        c
        ompareDate.A
        d
        d
        Years(1) - com
        p
        areDa
        t
        e)        
  
             var r
        u
        le1 = new LifecycleRule
        (
        ull, "txt", exp, null,
 
         
                 new RuleFilter(null, "txt/", 
        nu         null, nul
        l
        , Li
        f
        cycleRul
        e
        .LI
        E
        YCLE_RULE
        _
        TATU
        S
        ENAB
        L
        E
        D
 
         
         
             );
        rules.Add
        r
        ule1);
        var lfc 
         n
        ew Lifecycle        n(r
        les);
    
         
           try
        {
      
         
            var lfcArgs = new Se
        t
        ucketLifecycleArgs()
                .
        Wi        ame)
     
         
          
         
              .W
        i
        thL
        f
        cycleConf
        i
        ur
        a
        tion(lf
        c
        ;

         
                
         
         
         
        wait
         
        inio
        .
        S
        etB
        u
        c
        ketLifecycleAsync(lfcArgs
        .
        Configure
        wa
        it(false);
         ew 
        intLogger(
        n
        ameof(BucketLifecycleAs
        y
        c_Test1) + ".1", setBuck
        e
        LifecycleSignature,
                  
                  etBucketLi
        f
        ecyc
        l
        Async pa
        s
        ses
        ,
        TestStatu
        s
        PA
        S
        S, Date
        T
        me
        .
        Now - st
        a
        r
        t
        ime,
        

            
         
         
           
         
         
                  args)
        
                .Log();
        }
           NotIm
        lemented
        E
        xcept
        i
        n ex)
    
         
         
          {
          
         
         new 
        M
        intLogger(na        (Bu        LifecycleAsy        est1) + ".1", setBucketLif        leSignat
        re,
  
             
            
          "Tests whether SetBucket
        L
        ifecycleAsy
        c pas
        se        TestStatus.NA
         DateTime
        N
        w - star
        t
        Tim
        e, ex.Message
        
         
         
            ex.ToStri
        n
        g(
        )
        , args: args)
        Log(
        ;
           
            }
    
         
           cat
        c
         (Exce
        ption ex)
           
              await 
        T
        arDown(min
        o, bucketNa
        me).ConfigureAwait(false           ne
         MintLogge
        r
        (name
        o
        (BucketLif
        e
        c
        ycleAsync_Test
        1
        ) + "
        .
        1", setBucketLifecycleSig
        a
        ture,
   
          
                  "T         SetB
        cketLife
        c
        ycleA
        s
        nc passes"
        ,
         
        TestStatus.FAI
        L
        , Dat
        e
        Ti        rtT
        me, ex.Mes
        s
        age,
 
         
                      ex.ToString(
        )
        ,
        args: args).Log();
        
                   }

        try
        {
            var lfcA
        r
        s = new Ge
        t
        Buck
        e
        Lifecycl
        e
        Arg
        (
        
        
         
          
         
           .Wit
        hB        );
        

                
         
         
         
        var 
        l
        cObj
         
        =
         aw
        a
        i
        t         ketLi
        fecycleAsyn(lfcArgs).Co
        figur
        A
        ait
        fals
        e
        );
          
         
         
        A
        ssert.IsNotNu
        l(l
        c
        bj)
        
         
         
          Assert
        .
        IsN
        o
        tNull(lf
        c
        O
        b
        j
        .Rules);
    
               Asse
        t
        IsT
        ue(lfcOb
        j
        .Rules.C
        o
        unt
         
        > 0)
        ;
                
         
           
        A
        ssert
        .
        reEqual(
        l
        fcO
        b
        j.R
        u
        e
        s
        C
        o
        n
        t
        , lfc.Rules.C
        unt);
   
         
         
             var lf
        c
        Date = D
        a
        t
        e
        i
        e.Parse(lfc
        O
        b
        j.Rules[0
        ]Expiration.D
        te, n
        l
        , D
        teTimeStyles.
        R
        ound
        t
        ipKin
        d
        ;
 
         
            
                  eEq
        al(Math.Fl
        o
        or((
        l
        cDate 
        -
        comp
        a
        re        ays)
        ,
        expI
        n
        ays);
       
         
            new MintLogger(nameof(BucketLifecyc
        leAsync_Test1) 
        +
         ".
        2
        ", ge
        t
        BucketLifecyc
        eSi
        n
        tur
        ,
                    
        "
        Tests
         
        whether GetBucketLifecyc        es"
         TestSt
        t
        s.P
        SS, DateTime.Now - sta
        r
        tT         
                ar
        g
        s: args)
 
                  g
        ();
        }
        catc
        h
         (N
        o
        tI        cepti
        n ex)
        

                {
            n
        e
        w MintL
        o
        g
        ger(nameof(Buc
        k
        etLif
        e
        cy        t1)
        + ".2", ge
        t
        Bucket
        L
        ifecycleSignature,
       
         
         
            
         
        Tests whether GetBucketLife
        cy        stStatus.NA, DateTime.Now - startTime, ex.Mess
        a
        e,
       
         
            
         
          ex.ToS
        t
        rin
        (
        , args: a
        rg            
         
          ca
        tc        

           
         
         
           {
            await Te
        r
        Down(minio, bucketName)
        Co
        nfigureAwait           
             new M
        i
        ntLogg
        e
        r(nameof(BucketLifecycleAs
        y
        c
        Test
        1
         + ".2", getBucketLifecycle
        Si                 "Tests whether GetBucketLifecycleAsyn
        c
        passes", T
        e
        st
        S
        atus.FAI
        L
        , D
        t
        Time.Now 
        -
        st
        a
        rtTime,
         e          
         
                
        e
        x
        .
        oStr
        i
        g(),
         
        a
        rgs
        :
         
        args).Log();
            
        h
        row;
    
          
         }

                {
   
                
        v
        ar lf
        c
        rgs = new 
        R
        e
        moveBucketLife
        c
        ycleA
        r
        gs           
           .WithBu
        c
        ket(bu
        c
        ketName);
            awai
        t
        m
        nio.
        R
        moveBucketLifecycleAsync(lf
        cA        ait(false);
            var getLifecycleArgs =
         
        ew GetBuck
        e
        tLif
        e
        ycleArgs
        (
        )
 
         
                 
         
         .
        W
        ithBuck
        et          
         
               v
        a
        r
         
        fcOb
        j
        = aw
        a
        i
        t m
        i
        n
        io        fecyc
        leAsync(getifecycleArgs).Configure        ;
 
              }
         
           
          catch (NotImplemente
        d
        Ex         
           {
     
         
              new 
        M
        in        of(
        ucketL
        f
        cycle
        sync_
        T
        est1) + ".3", deleteBuc
        k
        etLifec
        y
        c
        leSignature,
 
         
             
         
                   wheth
        e
        r RemoveB
        u
        cketLi
        f
        ec        sses",
         
        TestStatu
        s
        .NA, D
        a
        teTim
        e
        .N        me, ex
        .
        Messag
        e
        ,
    
         
             
         
             
        x
        T
        o
        St        : args
        )
        .Log();

         
              
         
        }
   
         
            c
        a
        ch 
        (
        Excep
        t
        ion e
        x
        )
           
              i
         
        ex.Messa
        g
        e.Con
        t
        ains("
        T
        he li
        f
        e
        c
        y
        cle config
        u
        rati
        o
         doe
        s
        not exist"))
 
         
                  {
 
         
                  new Mi
        n
        tLogger(
        n
        ameo
        f
        (Buck
        e
        t
        Lifecyc
        e
        sync_Test1)
         
        +
         ".3", de
        l
        e
        eBucketLi
        f
        ec        e,

                  
         
              
         
         "Tests whether RemoveBuck
        e
        L
        fecy
        c
        eAsync passes", TestStatus.
        PA        tartTime,
                    args: args).Log(
        )
        
         
         
          }

         
                
         
         el
        e
                 
                  ew M
        i
        tLog
        ge        i
        fec
        y
        c
        leAsync_Test1) + ".3", de
        e
        teBucketLifecycleSignat
        re
        ,
                  est
         whether R
        e
        moveBu
        c
        ketLifecycleAsync passes",
         
        e
        tSta
        t
        s.FAIL, DateTime.Now - star
        tT                 ex.Message, ex.ToString(), args: args
        )
        Log();
   
         
          
         
                
        t
        hro
        ;
                 
         
         }
        

               
         }        
 
         
              {

         
         
         
            
         
          aw
        a
        i
        t T
        e
        a
        rDown(minio, bucketName).
        o
        nfigureAw
        it
        (false);
        }
    }

        internal static async Task BucketLifecycleAsync_Test2(MinioClient minio)
        
               var star        tT        m
        e         =         DateTime.Now;
               
        ar
         cketName =         e(        15        )
        
                        v        ar 
        a
        rgs = 
        n
        ew D        ic        tionar        y<        string, string>

                   
           {
        

                                   {         "        bucketName", buck
        e
            t
        y
     
          {
                    await Se
        up_Tes
        t(minio, bucketName).ConfigureAwait(false);
                }
         
         
         e
        x
        )
                                
          
            
         
                  awa
        i
        t
         Te
        a
        r
        Down(minio,
        ).C        onf
        g
        ureAwait(
        al
        s
                  ger(n
        meo        (B        ucke
        t
        Lifec
        y
        leAsync_Te
        st        ),         setBucketLif
        e
        cycle
        S
        i
           
                               "Tes        ts         
        w
        hether
         
        SetBucketLifecycle        As        nc        passe        s"        T
        stSt
        a
        us.FAIL, DateTime.N        ow - star
        tT        
                ex.ToString(), args:         ar        s).Log
        ();
                                           t
        h
        row;        
          
                            

  
         
                    va         ru        le        s = ne
        w         is
        t
        <Lifecy
        c
        ar         e        xp        = new         E        p
        i
        r
        a
        n
                     
        {
  
         
         
           
         
         
         
                     
        }
        vr rule1 = new Lifecycl
        t"        , 
        xp,         n        ll,
         
                                         new RuleFilter        (n        ull, "        tx        t/
        "
        ,
         
         null,         nu        ll,
         
        Lif        ec        ycleRul
        e
        .L        E_STA        TU        _E        ABL
        ED        
               );
        rules.A
        d
        d(r        ule1)
        ;
        

                var lf
        c
         = ne
        w
         L        igu
        ation(rules);
  
         
        tr
        
                {        
                           var         lf        c
        if        ec        ycleArgs        ()
        

                  
         
         
        (b        uc        k
        me)
                    
                                        
         .        Wi        th        Li
        f
        ecycleConfigurati        on(lfc)
        ;
        
            awa
        i
        t
         minio.SetBuck
        e
        tLife
        c
        y
        f
        gureA
        a
        it(false);
                            ne
         M
        i
        a
        cyce        As        ync_Test2
        )
         + ".1
        ",        setBuck        tL        fecycl        Signat        ur        e,
 
         
         
            
         
                 "        Te        sts whet        he        r         SetBucketLi
        f
        TestStatus.PA        SS        ,
        DateTime        .N        ow - st        ar        tTime,
    
              
          
            args:         ar        g
        s)
        

                                                     
          .
        o
        ();
     
         
         }
                        c        at        c
        io         e        x)        
     
         
         
         
        
   
         
                               n
        ew 
        M
        i
        n
        f
        cy        cl        Asn        c_        Test2)         +         ".
        ",         s        etBucketLif
        e
          
         
                                 
         
        "Tes        ts         whe
        t
        he        r         Se
        Buc        ke        tL        if        ec        ycl
        Async p        as        ses",         T        tSta        tu        .NA
         Date
        Tim
        m
           
                                ex.T
        oS        tr        ing()        ,         a
        rgs:         ar        gs        ).Log();                       }
   
         
         c        tch 
        (
        xce        pt        io         ex)
                        {
            
        a
        tName).ConfigureAwait(fals        e)        
                                  new 
        intLog
        ger(nameof(Buck        etLi
        f
        cycleAsy
        n
        c_T
        s
        2) + ".1"
        ,         gnat
        u
        e,
 
         
         
           
                   
        t
        cyc        e
        Te        ate
        im        e.        ow -         sta
        r
        tTime,
         
        ex.Messag        e,
                                        ex
        .
        o
        trin
        g
        ),         rg        s: args).Log();
                           th            tr               
        {
                                            var lfcArgs = ne
         GetBu
        ck
        fe        ycleA        rg        (
        )
        
   
         
                
         
          .
        i
        hBuck        et(bu
        ck         v
        a
        r lfcOb
        j
        = 
        a
        wait min
        i
        o
        .
        etBu
        c
        etLif        ec        cle
        A
        s
        y
        wa        it(f        l
         
        Null(lfcObj);
   
        s
        j.R        ul        es        )
        
                                  
         
         Asse
        r
        .IsTrue(lf
        c
        Ob        .R        ules.Count        > 0        )
        ;
   
         
                Asse        rE        (lfcObj.
        ules.C
        unt, 
        fc.R
        les.Count);
            ne
        w
         MintLogger
        nameo
        f(        etLifecycleAs
        nc_Test2)
        +
        ".2", ge
        t
        Buc
        ketLifecycleS
        gnature,
 
         
                     
         
          
        "
        Tests whether
        GetB
        c
        etL
        fecycleAsy
        n
        c pass
        e
        ", Tes
        tStatus.PASS        o
         - startTime
        ,
                  
                 ar
        gs: args)
                           
        }
        
        c
        atch 
        (
        otImplemen
        t
        e
        dException ex)
        

             
         
          {
            new MintL
        g
        ger(nameo
        (B
        ucketLifecyc        2) + 
        .2", get
        B
        ucket
        L
        fecycleSig
        n
        a
        ture,
        
         
             
         
         "        r G
        tBucketLif
        e
        cycleA
        s
        ync passes", TestStatus.NA
        ,
         
        ateTime.Now - startTime, ex
        .M                ex.ToString(), args: args).Log();
    
         
          }
      
         
         cat
        c
         (Except
        i
        on 
        x
        
        
        {
          
         
               
                  in
        i
        o, bucke
        t
        N
        a
        e).C
        o
        figu
        r
        e
        Awa
        i
        t
        (f             
          new MintLgger(nameof(
        ucket
        i
        ecy
        leAs
        y
        nc_Test2) + "
        .
        2
        "
        , getBucketLi
        ecy
        l
        Sig
        ature,
                      ther
        G
        tBucketLifec
        yleAsync pass
        s", T
        s
        Sta
        us.FAIL, Date
        T
        ime.
        N
        w - s
        t
        rtT
        i
        e, e
        x.           
                 e
        x
        .ToS
        t
        ing(),
         
        rgs:
         
        ar            
         
            
         
        hrow;
       
         
        }

        try
        {
            va
        r lfcArgs = new
         
        Rem
        o
        veBuc
        k
        etLifecycleAr
        s()
         
           
                  .WithBucket(
        b
        ucket
        N
        ame);
            await         Buc
        etLifec
        c
        eAs
        nc(lfcArgs).ConfigureA
        w
        ai         
             var g
        e
        tLifecycle
        Ar        e
        tLifecycleArgs()
         
         
           
         
                  bucke
        Name)
        ;
        
            var lfcObj
         
        = await
         
        m
        inio.GetBucket
        L
        ifecy
        c
        le        ecy
        leArgs).Co
        n
        figure
        A
        wait(false);
        }
   
         
         
         cat
        c
         (NotImplementedException e
        x)           new MintLogger(nameof(BucketLifecycleAsync_
        T
        st2) + ".3
        "
        , de
        l
        teBucket
        L
        ife
        y
        leSignatu
        re        ests
         
        heth
        er        e
        cyc
        l
        e
        Async passes", TestStatus
        N
        A, DateTime.Now - start
        im
        e, ex.Messag           
           ex.ToSt
        r
        ing(),
         
        args: args).Log();
       
         
        

            
         
         catch (Exception ex)
     
                  f (ex.Message.Contains("The lifecycle configur
        a
        ion does n
        o
        t 
        e
        ist"))
 
         
           
         
            {
   
         
          
         
               
         n        me
        o
        f(Bucket
        L
        i
        f
        cycl
        e
        sync
        _
        T
        est
        2
        )
         + ".3", deleteBucketLife
        y
        cleSignat
        re
        ,
                  ests 
        hether R
        e
        moveB
        u
        ketLifecyc
        l
        e
        Async passes",
         
        TestS
        t
        at        eTi
        e.Now - st
        a
        rtTime
        ,
        
                    args:
         
        r
        s).L
        o
        ();
            }
         
                    {
                new MintLogger(nameof(Buck
        e
        LifecycleA
        s
        ync_
        T
        st2) + "
        .
        3",
        d
        leteBucke
        t
        if
        e
        cycleSi
        gn          
         
                
        "
        T
        e
        ts w
        h
        ther
         
        R
        emo
        v
        e
        Bu        eAsyn
        c passes", estStatus.FAIL, DateTim        tTi
        e,
    
         
           
                 ex.Message, e
        x
        .T         
        args).Log(
        )
        ;
        
         
                     
              
         
        
    
           }

         
               finally
        
        {
        
      
         
         
            await Tear
        D
        own(m
        i
        ni        e).Con
        f
        igureAwai
        t
        (false
        )
        ;
          }

        #endregion
    }
}