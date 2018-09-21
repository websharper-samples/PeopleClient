namespace PeopleClient

open WebSharper
open WebSharper.Mvu
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Client
open PeopleApi.App.Model

/// Display the application based on the state.
[<JavaScript>]
module View =

    /// Parses index.html to extract templates.
    type Template = Templating.Template<"wwwroot/index.html", ClientLoad.FromDocument>

    module Common =

        /// Parameters that distinguish the Creating and Editing pages.
        type EditFormParams =
            {
                SubmitText: string
                SubmitMessage: Message
            }

        /// Attribute that makes a button disabled and show a loader
        /// when the application is refreshing.
        let DisabledWhenRefreshing (state: View<State>) : Attr =
            Attr.Concat [
                Attr.Prop "disabled" state.V.Refreshing
                Attr.ClassPred "is-loading" state.V.Refreshing
            ]

        /// The editor form used by the Creating and Editing pages.
        let EditForm (dispatch: Dispatch<Message>) (state: View<State>) (param: EditFormParams) =
            let dispatchEditing msg = dispatch << UpdateEditing << msg
            Template.Form()
                .FirstName(V state.V.Editing.FirstName, dispatchEditing SetFirstName)
                .LastName(V state.V.Editing.LastName, dispatchEditing SetLastName)
                .Born(V state.V.Editing.Born, dispatchEditing SetBorn)
                .Died(V state.V.Editing.Died, dispatchEditing SetDied)
                .HasDied(V state.V.Editing.HasDied, dispatchEditing SetHasDied)
                .DiedAttr(Attr.Prop "disabled" (not state.V.Editing.HasDied))
                .Submit(fun e ->
                    dispatch param.SubmitMessage
                    e.Event.PreventDefault()
                )
                .SubmitText(param.SubmitText)
                .SubmitAttr(DisabledWhenRefreshing state)
                .Back(fun _ -> dispatch (Goto PeopleList))
                .Doc()

    module EditPerson =

        /// The page to edit a person's data.
        /// Uses Page.Create to make a page that is indexed by PersonId.
        let Page = Page.Create(fun pid (dispatch: Dispatch<Message>) (state: View<State>) ->
            Common.EditForm dispatch state {
                SubmitText = "Edit"
                SubmitMessage = SubmitEditPerson pid
            }
        )

    module CreatePerson =

        /// The page to create a new person.
        /// Uses Page.Single to create a single instance of the page.
        let Page = Page.Single(fun (dispatch: Dispatch<Message>) (state: View<State>) ->
            Common.EditForm dispatch state {
                SubmitText = "Create"
                SubmitMessage = SubmitCreatePerson
            }
        )

    module PeopleList =

        /// Display a person's data in a table row.
        let PersonRow dispatch (state: View<State>) (pid: PersonId) (person: View<PersonData>) =
            Template.Row()
                .FirstName(person.V.firstName)
                .LastName(person.V.lastName)
                .Born(person.V.born.ToShortDateString())
                .Died(
                    match person.V.died with
                    | None -> ""
                    | Some d -> d.ToShortDateString()
                )
                .Edit(fun _ -> dispatch (Goto (Editing pid)))
                .EditAttr(Common.DisabledWhenRefreshing state)
                .Delete(fun _ -> dispatch (RequestDeletePerson pid))
                .DeleteAttr(Common.DisabledWhenRefreshing state)
                .Doc()

        /// The page for the list of people.
        /// Uses Page.Single to create a single instance of the page.
        let Page = Page.Single(fun (dispatch: Dispatch<Message>) (state: View<State>) ->
            Template.Table()
                .Body((V state.V.People).DocSeqCached(PersonRow dispatch state))
                .Create(fun _ -> dispatch (Goto Creating))
                .CreateAttr(Common.DisabledWhenRefreshing state)
                .Refresh(fun _ -> dispatch (RefreshList false))
                .RefreshAttr(Common.DisabledWhenRefreshing state)
                .DeleteModalAttr(Attr.ClassPred "is-active" state.V.Deleting.IsSome)
                .DeleteConfirm(fun _ -> dispatch ConfirmDeletePerson)
                .DeleteConfirmAttr(Common.DisabledWhenRefreshing state)
                .DeleteCancel(fun _ -> dispatch CancelDeletePerson)
                .Doc()
        )

    /// Decide which page to display based on the application state.
    /// Note that the Page.* functions above create cached pages,
    /// their content is not called again on every state change.
    let Page state =
        match state.Page with
        | PeopleList -> PeopleList.Page ()
        | Creating -> CreatePerson.Page ()
        | Editing pid -> EditPerson.Page pid
