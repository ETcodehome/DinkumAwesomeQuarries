Built following guide on
https://modding.wiki/en/dinkum
https://modding.wiki/en/dinkum/DinkumTutorialBuildEnvironment

Harmony patching info from
https://harmony.pardeike.net/articles/patching-postfix.html

Care with the references in csproj - 
There is an absolute ref in there that needs updating to match the game directory location.

To compile, navigate to the project directory and run in a terminal window
dotnet build
Compiling will create a bin\Debug\netstandard2.0 folder in the project directory
with a dll named <modname>.dll

Decompile reference code using dnspy.
