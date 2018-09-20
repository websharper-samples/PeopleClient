namespace PeopleClient

open WebSharper
open WebSharper.Mvu
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Client
open PeopleApi.App.Model

[<JavaScript>]
module View =

    type Template = Templating.Template<"wwwroot/index.html", ClientLoad.FromDocument>

    module Common =

        type EditFormParams =
            {
                SubmitText: string
                SubmitMessage: Message
            }

        let DisabledWhenRefreshing (state: View<State>) =
            Attr.Concat [
                Attr.Prop "disabled" state.V.Refreshing
                Attr.ClassPred "is-loading" state.V.Refreshing
            ]

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

        let Page = Page.Create(fun pid (dispatch: Dispatch<Message>) (state: View<State>) ->
            Common.EditForm dispatch state {
                SubmitText = "Edit"
                SubmitMessage = SubmitEditPerson pid
            }
        )

    module CreatePerson =

        let Page = Page.Single(fun (dispatch: Dispatch<Message>) (state: View<State>) ->
            Common.EditForm dispatch state {
                SubmitText = "Create"
                SubmitMessage = SubmitCreatePerson
            }
        )

    module PeopleList =

        let PersonRow dispatch (state: View<State>) (pid: int) (person: View<PersonData>) =
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
                .Delete(fun _ -> dispatch (SubmitDeletePerson pid))
                .DeleteAttr(Common.DisabledWhenRefreshing state)
                .Doc()

        let Page = Page.Single(fun (dispatch: Dispatch<Message>) (state: View<State>) ->
            Template.Table()
                .Body((V state.V.People).DocSeqCached(PersonRow dispatch state))
                .Create(fun _ -> dispatch (Goto Creating))
                .CreateAttr(Common.DisabledWhenRefreshing state)
                .Doc()
        )

    let Page state =
        match state.Page with
        | PeopleList -> PeopleList.Page ()
        | Creating -> CreatePerson.Page ()
        | Editing pid -> EditPerson.Page pid
