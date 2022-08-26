open System.CommandLine
open System.CommandLine.Invocation
open System.CommandLine.NamingConventionBinder
open System.IO
open System.Xml

// TODO: construct Diagnostic ID -> Target mapping

let diff (txt: string) (xml: string) =
    let lines = File.ReadLines(txt) |> Seq.skip 1 |> Seq.length
    printfn $"{txt} has {lines} entries"
    let doc = new XmlDocument()
    doc.Load xml
    let nodes = doc.SelectNodes "/Suppressions/Suppression"
    printfn $"{xml} has {nodes.Count} entries"

let [<EntryPoint>] main args =
    let root = new RootCommand()
    let txt = new Option<string>("--txt")
    txt.IsRequired <- true
    let xml = new Option<string>("--xml")
    xml.IsRequired <- true
    root.AddOption txt
    root.AddOption xml
    root.Handler <- CommandHandler.Create(fun (r: {| txt: string; xml: string |}) -> diff r.txt r.xml)
    root.Invoke args