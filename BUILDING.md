
This is an open-source (AGPL) project, but for the time being it still relies on 
proprietary tools.

General restrictions / notes:

1.  The projects are currently set to build in Visual Studio 2008, and no other 
    build environments (eg SharpDevelop, MonoDeveop) have been tested.
2.  Assembly Merging requires that all assemblies in the dependency tree resolve 
    correctly during the merge process, even when the assemblies are unused in the 
    final executable; DotNetOpenAuth has a reference to ASP.Net MVC 1.0, so we also 
    need to have ASP.Net MVC 1.0 installed on the build machine in order to 
    successfully "Merge" the output into a single executable assembly:
    http://www.microsoft.com/en-us/download/details.aspx?id=5388
3.  Some paths in the solution are still hardcoded to my own full paths, 
    because I could not find a good way to get Visual Studio to recognize relative 
    paths in these spots. I don't think this is a big issue, the issue is pretty 
    easy to find/correct for any given environment.
4.  The solution MUST be stored in a path without spaces for some things (eg 
    Assembly Merging) to work.

Instructions:

1.  Clone from github
2.  get SubModule(s) with "git submodule update --init" MORE WORK TO BE DONE HERE - PUBLIC REPO INSTEAD OF PRIVATE
3.  Rename "HeapsTodoSyncTool/Constants.cs.sample" to "HeapsTodoSyncTool/Constants.cs",
    and replace the strings in the file with more reasonable values obtained from 
    Google's 
4.  Open in Visual Studio + build the solution; there should be nothing else to it.

COMING SOON: MonoDevelop support on Linux
