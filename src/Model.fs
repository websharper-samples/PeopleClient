namespace PeopleClient

open System
open WebSharper
open WebSharper.JavaScript
open PeopleApi.App.Model

type PersonId = int

/// An identifier for the page being shown.
type Page =
    | [<EndPoint "/">] PeopleList
    | [<EndPoint "/create">] Creating
    | [<EndPoint "/edit">] Editing of PersonId

/// Contents of the fields of the Creating and Editing pages.
type PersonEditorState =
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
        Editing: PersonEditorState
        Deleting : option<PersonId>
    }

/// Operations on a person editor state.
[<JavaScript>]
module PersonEditorState =

    /// Convert a date to a string using the same format as <input type="date">.
    let DateToString (date: DateTime) : string =
        sprintf "%04i-%02i-%02i" date.Year date.Month date.Day

    /// Convert a date from a string using the same format as <input type="date">.
    let TryParseDate (s: string) : option<DateTime> =
        let d = JavaScript.Date(s)
        if Number.IsNaN (d.GetTime()) then
            None
        else
            Some d.Self

    /// Create editor state from a person data.
    let OfData (p: PersonData) : PersonEditorState =
        {
            FirstName = p.firstName
            LastName = p.lastName
            Born = DateToString p.born
            HasDied = Option.isSome p.died
            Died = match p.died with None -> "" | Some d -> DateToString d
        }

    /// Extract person data from an editor state.
    let TryToData (id: PersonId) (p: PersonEditorState) : option<PersonData> =
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

    /// Empty editor state.
    let Init =
        {
            FirstName = ""
            LastName = ""
            Born = ""
            HasDied = false
            Died = ""
        }

/// Operations on application state.
[<JavaScript>]
module State =

    /// Initial application state.
    let Init : State =
        {
            People = Map.empty
            Refreshing = true
            Error = None
            Page = PeopleList
            Editing = PersonEditorState.Init
            Deleting = None
        }
