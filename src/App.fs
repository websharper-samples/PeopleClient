namespace PeopleClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Client
open WebSharper.Mvu
open WebSharper.Sitelets.InferRouter
open PeopleApi.App.Model

[<JavaScript>]
module App =

    /// The router for the client application's local pages.
    let router = Router.Infer<Page>()

    /// Startup function for the application.
    [<SPAEntryPoint>]
    let Main () =
        App.CreatePaged State.Init Update.UpdateApp View.Page
        |> App.WithCustomRouting router (fun s -> s.Page) Update.Goto
        |> App.WithInitMessage (RefreshList false)
        |> App.WithLog (fun msg model ->
            Console.Log (Json.Encode msg)
            Console.Log (Json.Encode model)
        )
        |> App.Run
        |> Doc.RunById "main"
