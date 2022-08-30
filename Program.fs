open System.CommandLine
open System.CommandLine.Invocation
open System.CommandLine.NamingConventionBinder
open System.IO
open System.Xml

let exclude predicate source =
    Seq.filter (fun x -> not (predicate x)) source

let trimPrefix (s: string) (prefix: string) =
    if s.StartsWith(prefix) then
        s.Substring(prefix.Length)
    else
        s

let typesMustExist s =
    let s = trimPrefix s "TypesMustExist : Type '"
    ("CP0001", s.Split('\'')[0])

let membersMustExist s = ("CP0002", "foo")

// TODO: construct Diagnostic ID -> List Target mapping
let cci_did =
    Map
        [ ("TypesMustExist", "CP0001")
          ("MembersMustExist", "CP0002")
          ("CannotRemoveAttribute", "CP0014")
          ("ParameterNamesCannotChange", "CP0017")
          ("CannotChangeAttribute", "CP0015") ]

let getMapping (lines: seq<string>) =
    lines
    |> Seq.map (fun s ->
        if s.StartsWith "TypesMustExist" then typesMustExist s
        elif s.StartsWith "MembersMustExist" then membersMustExist s
        else ("a", "b"))
    |> Seq.fold
        (fun (m: Map<string, string list>) (elem: string * string) ->
            (Map.change
                (fst elem)
                (fun x ->
                    let v = snd elem

                    match x with
                    | Some old -> Some(v :: old)
                    | None -> Some([ v ]))
                m))
        Map.empty

let diff (txt: string) (xml: string) =
    let lines =
        File.ReadLines(txt)
        |> Seq.map (fun s -> s.TrimStart [| '-'; ' ' |])
        |> exclude (fun s -> s.StartsWith "#")
        |> exclude (fun s -> s.StartsWith "Total Issues")
        |> exclude (fun s -> s.StartsWith "Compat issues")
        |> exclude (fun s -> s.Trim() = "")

    printfn "%A" (getMapping lines)
    printfn $"{txt} has {Seq.length lines} entries"
    let doc = new XmlDocument()
    doc.Load xml
    let nodes = doc.SelectNodes "/Suppressions/Suppression"
    printfn $"{xml} has {nodes.Count} entries"

[<EntryPoint>]
let main args =
    let root = new RootCommand()
    let txt = new Option<string>("--txt")
    txt.IsRequired <- true
    let xml = new Option<string>("--xml")
    xml.IsRequired <- true
    root.AddOption txt
    root.AddOption xml
    root.Handler <- CommandHandler.Create(fun (r: {| txt: string; xml: string |}) -> diff r.txt r.xml)
    root.Invoke args
