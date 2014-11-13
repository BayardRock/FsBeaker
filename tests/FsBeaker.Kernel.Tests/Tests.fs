namespace FsBeaker.Kernel.Tests

open System.Reflection
open FsBeaker.Kernel
open NUnit.Framework
open System
open System.Text

[<TestFixture>]
type Tests() = 

    let codeAndLocation(str:string) = 
        let findString = "||"
        let lines = str.Split('\n')
        let lineIndex = lines |> Array.findIndex (fun x -> x.Contains(findString))
        let charIndex = lines.[lineIndex].IndexOf(findString)
        let newCode = str.Replace(findString, "")
        newCode, lineIndex, charIndex
    
    [<Test>]
    member __.TestIntellisense() =
        
        // check extract names
        let code, _, charIndex = codeAndLocation "a.b.c.d.e.f.g||"
        match extractNames(code, charIndex) with
        | Some(names, _) -> Assert.AreEqual([|"a"; "b"; "c"; "d"; "e"; "f"|], names)
        | None           -> Assert.Fail("Names should be returned")

        // check extract names
        let code, _, charIndex = codeAndLocation "a.||b.c.d.e.f.g"
        match extractNames(code, charIndex) with
        | Some(names, _) -> Assert.AreEqual([|"a"|], names)
        | None           -> Assert.Fail("Names should be returned")

        // check GetDeclarations
        let code, lineIndex, charIndex = codeAndLocation "1 |> fun x -> System.String().||"
        let decls, _ = Evaluation.GetDeclarations(code, lineIndex, charIndex)
        Assert.AreEqual(35, decls.Length)