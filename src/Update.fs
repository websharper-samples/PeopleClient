namespace PeopleClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.Mvu
open WebSharper.Sitelets
open PeopleApi.App.Model
open PeopleClient

[<NamedUnionCases>]
type PersonUpdateMessage =
    | SetFirstName of firstName: string
    | SetLastName of lastName: string
    | SetBorn of born: string
    | SetDied of died: string
    | SetHasDied of hasDied: bool

[<NamedUnionCases "type">]
type Message =
    | Goto of page: Page
    | UpdateEditing of msg: PersonUpdateMessage
    | RefreshList of gotoList: bool
    | SubmitCreatePerson
    | SubmitEditPerson of id: int
    | SubmitDeletePerson of id: int
    | ListRefreshed of people: PersonData[] * gotoList: bool
    | Error of error: string

[<JavaScript>]
module Update =

    let [<Literal>] BaseUrl = "http://localhost:5000"

    let route = Router.Infer<EndPoint>()

    let DispatchAjax (endpoint: ApiEndPoint) (parseSuccess: string -> Message) =
        // We use UpdateModel because SetModel would "overwrite" changes
        // previously done with SetModel.
        UpdateModel (fun state -> { state with Refreshing = true })
        +
        CommandAsync (fun dispatch -> async {
            try
                // Delay command so that we can see loading animations
                do! Async.Sleep 1000
                let! res = Promise.AsAsync <| promise {
                    let! ep =
                        EndPoint.Api endpoint
                        |> Router.FetchWith (Some BaseUrl) (RequestOptions()) route
                    return! ep.Text()
                }
                dispatch (parseSuccess res)
            with e ->
                dispatch (Error e.Message)
        })

    let UpdatePerson (message: PersonUpdateMessage) (state: PersonEditing) =
        match message with
        | SetFirstName s -> { state with FirstName = s }
        | SetLastName s -> { state with LastName = s }
        | SetBorn s -> { state with Born = s }
        | SetDied s -> { state with Died = s }
        | SetHasDied b -> { state with HasDied = b }

    let Goto (page: Page) (state: State) : State =
        match page with
        | PeopleList ->
            { state with Page = PeopleList }
        | Creating ->
            { state with
                Page = Creating
                Editing = State.Init.Editing
            }
        | Editing pid ->
            { state with
                Page = Editing pid
                Editing =
                    Map.tryFind pid state.People
                    |> Option.map PersonEditing.OfData
                    |> Option.defaultValue state.Editing
            }

    let UpdateApp (message: Message) (state: State) =
        match message with
        | Goto page ->
            UpdateModel (Goto page)
        | UpdateEditing msg ->
            SetModel { state with Editing = UpdatePerson msg state.Editing }
        | RefreshList gotoList ->
            DispatchAjax ApiEndPoint.GetPeople
                (fun res -> ListRefreshed (Json.Deserialize res, gotoList))
        | SubmitCreatePerson ->
            match PersonEditing.TryToData 0 state.Editing with
            | None ->
                SetModel { state with Error = Some "Invalid person" }
            | Some editing ->
                DispatchAjax (ApiEndPoint.CreatePerson editing)
                    (fun _ -> RefreshList true)
        | SubmitEditPerson id ->
            match PersonEditing.TryToData id state.Editing with
            | None ->
                SetModel { state with Error = Some "Invalid person" }
            | Some editing ->
                DispatchAjax (ApiEndPoint.EditPerson editing)
                    (fun _ -> RefreshList true)
        | SubmitDeletePerson pid ->
            DispatchAjax (ApiEndPoint.DeletePerson pid)
                (fun _ -> RefreshList true)
        | ListRefreshed (people, gotoList) ->
            let people = Map [ for p in people -> p.id, p ]
            let page = if gotoList then PeopleList else state.Page
            SetModel {
                state with
                    Page = page
                    Refreshing = false
                    People = people
                    Error = None
                    Editing =
                        match page with
                        | Editing id ->
                            Map.tryFind id people
                            |> Option.map PersonEditing.OfData
                            |> Option.defaultValue state.Editing
                        | _ -> state.Editing
            }
        | Error err ->
            SetModel { state with Refreshing = false; Error = Some err }
