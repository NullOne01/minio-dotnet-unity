import re
import glob

root_dir = input()
# F:\Projects\Unity\visual-novel-engine\UwUNovelsEngine\Assets\Scripts\Utilities\Network\minio-dotnet-unity\Scripts\Test
# root_dir needs a trailing slash (i.e. /root/dir/)
for filename in glob.iglob(root_dir + '**/*.cs', recursive=True):
    print(filename)

    content_new = ""
    with open(filename, 'r') as f:
        content = f.read()
        content_new = content
        content_new = re.sub(r'(namespace [^;]*)(;)(.*)', r'\1 {\3 \n}', content, flags=re.S)
    
    new_filename = f"{filename}"
    print(new_filename)
    with open(new_filename, 'w') as fw:
        fw.write(content_new)
