Built following guides

https://modding.wiki/en/dinkum

https://modding.wiki/en/dinkum/DinkumTutorialBuildEnvironment

Harmony patching info from

https://harmony.pardeike.net/articles/patching-postfix.html

If working with the repo update the Bepinex and game path references in .csproj 

To compile, navigate to the project directory and run in a terminal window
dotnet build
Compiling will create a bin\Debug\netstandard2.0 folder in the project directory
with a dll named <modname>.dll

Decompile reference code using dnspy.
