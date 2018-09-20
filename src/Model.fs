namespace PeopleClient

open WebSharper
open WebSharper.JavaScript
open PeopleApi.App.Model

type Page =
    | [<EndPoint "/">] PeopleList
    | [<EndPoint "/create">] Creating
    | [<EndPoint "/edit">] Editing of int

type PersonEditing =
    {
        FirstName: string
        LastName: string
        Born: string
        HasDied: bool
        Died: string
    }

type State =
    {
        People: Map<int, PersonData>
        Refreshing: bool
        Error: option<string>
        Page: Page
        Editing: PersonEditing
    }

[<JavaScript>]
module PersonEditing =

    let DateToString (date: System.DateTime) =
        sprintf "%04i-%02i-%02i" date.Year date.Month date.Day

    let TryParseDate (s: string) =
        let d = JavaScript.Date(s)
        if Number.IsNaN (d.GetTime()) then
            None
        else
            Some d.Self

    let IsValid (p: PersonEditing) =
        Option.isSome (TryParseDate p.Born)
        && (not p.HasDied || Option.isSome (TryParseDate p.Died))

    let OfData (p: PersonData) =
        {
            FirstName = p.firstName
            LastName = p.lastName
            Born = DateToString p.born
            HasDied = Option.isSome p.died
            Died = match p.died with None -> "" | Some d -> DateToString d
        }

    let TryToData (id: int) (p: PersonEditing) =
        match TryParseDate p.Born with
        | None -> None
        | Some born ->
            match p.HasDied, TryParseDate p.Died with
            | true, None -> None
            | hasDied, died ->
                Some {
                    id = id
                    firstName = p.FirstName
                    lastName = p.LastName
                    born = born
                    died = if hasDied then died else None
                }

[<JavaScript>]
module State =

    let Init =
        {
            People = Map.empty
            Refreshing = true
            Error = None
            Page = PeopleList
            Editing =
                {
                    FirstName = ""
                    LastName = ""
                    Born = ""
                    HasDied = false
                    Died = ""
                }
        }
