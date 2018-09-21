namespace PeopleClient

open System
open WebSharper
open WebSharper.JavaScript
open PeopleApi.App.Model

type PersonId = int

type Page =
    | [<EndPoint "/">] PeopleList
    | [<EndPoint "/create">] Creating
    | [<EndPoint "/edit">] Editing of PersonId

/// Contents of the fields of the Creating and Editing pages.
type PersonEditing =
    {
        FirstName: string
        LastName: string
        Born: string
        HasDied: bool
        Died: string
    }

/// State of the full client app.
type State =
    {
        People: Map<PersonId, PersonData>
        Refreshing: bool
        Error: option<string>
        Page: Page
        Editing: PersonEditing
        Deleting : option<PersonId>
    }

[<JavaScript>]
module PersonEditing =

    let DateToString (date: DateTime) : string =
        sprintf "%04i-%02i-%02i" date.Year date.Month date.Day

    let TryParseDate (s: string) : option<DateTime> =
        let d = JavaScript.Date(s)
        if Number.IsNaN (d.GetTime()) then
            None
        else
            Some d.Self

    let OfData (p: PersonData) : PersonEditing =
        {
            FirstName = p.firstName
            LastName = p.lastName
            Born = DateToString p.born
            HasDied = Option.isSome p.died
            Died = match p.died with None -> "" | Some d -> DateToString d
        }

    let TryToData (id: PersonId) (p: PersonEditing) : option<PersonData> =
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

    let Init =
        {
            FirstName = ""
            LastName = ""
            Born = ""
            HasDied = false
            Died = ""
        }

[<JavaScript>]
module State =

    let Init : State =
        {
            People = Map.empty
            Refreshing = true
            Error = None
            Page = PeopleList
            Editing = PersonEditing.Init
            Deleting = None
        }
