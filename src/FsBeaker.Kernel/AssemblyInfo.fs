namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsBeaker.Kernel")>]
[<assembly: AssemblyProductAttribute("FsBeaker")>]
[<assembly: AssemblyDescriptionAttribute("F# implementation for beaker notebook")>]
[<assembly: AssemblyVersionAttribute("0.2")>]
[<assembly: AssemblyFileVersionAttribute("0.2")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2"
