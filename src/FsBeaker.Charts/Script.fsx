#r "../../packages/Newtonsoft.Json.6.0.6/lib/net40/Newtonsoft.Json.dll"
#load "Charts.fs"

open System
open Newtonsoft.Json
open FsBeaker.Charts

let data = [1..100] |> Seq.map (fun x -> x, x)

Plot()
    <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 1"))
    <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 2"))
    <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 3"))
    <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 4"))
    <|> (data |> BeakerChartBeta.Line (DisplayName = "Hello 5"))
