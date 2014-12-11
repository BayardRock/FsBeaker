namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsBeaker.Charts")>]
[<assembly: AssemblyProductAttribute("FsBeaker")>]
[<assembly: AssemblyDescriptionAttribute("F# implementation for beaker notebook")>]
[<assembly: AssemblyVersionAttribute("0.3.2")>]
[<assembly: AssemblyFileVersionAttribute("0.3.2")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.2"
