namespace XamarinStore

open System
open System.Collections.Generic
open System.Linq
open System.Text
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Util
open Android.Views
open Android.Widget
open Android.Text
open Android.Graphics
open Helpers

type LoginFragment() =
    inherit Fragment()

    // TODO Add your Xamarin account email address here (sign up at store.xamarin.com/account/register)
    //
    // In the C# version of this app, xamarinAccountEmail is just a string, with a default value of ""
    // to indicate that no email address has been entered. F# has a type called option<'T> precisely for
    // situations when a value may not be present. Here, XamarinAccountEmail is of type option<string>.
    // Add your email by replacing 'None' with 'Some "x"', where x is your email address!
    //
    let xamarinAccountEmail = None

    let mutable password:EditText = null
    let mutable login:Button = null
    let mutable imageView:ImageView = null

    member val LoginSucceeded = fun ()->() with get,set

    member this.Login usernameParam passwordParam = async {
        let progressDialog = ProgressDialog.Show (this.Activity, "Please wait...", "Logging in", true)
        login.Enabled <- false
        password.Enabled <- false
        let! success = WebService.Shared.Login usernameParam passwordParam
        if success then
            let! canContinue = WebService.Shared.PlaceOrder (WebService.Shared.CurrentUser, true)
            if not canContinue.Success 
            then Toast.MakeText(this.Activity,"Sorry, only one shirt per person. Edit your cart and try again.", ToastLength.Long).Show()
            else this.LoginSucceeded ()
        else Toast.MakeText(this.Activity, "Please verify your Xamarin account credentials and try again", ToastLength.Long).Show()

        login.Enabled <- true
        password.Enabled <- true
        progressDialog.Hide ()
        progressDialog.Dismiss () }

    member private this.CreateInstructions (inflater:LayoutInflater) container savedInstanceState =
        let view = inflater.Inflate (Resource_Layout.PrefillXamarinAccountInstructions, null)
        let textView = view.FindViewById<TextView> (Resource_Id.codeTextView)
        let coloredText = Html.FromHtml ("<font color='#48D1CC'>let</font> <font color='#1E90FF'></font> xamarinAccountEmail = ...")
        textView.SetText (coloredText, TextView.BufferType.Spannable)
        view

    member private this.LoadUserImage email = async {
        //Get the correct size in pixels
        let px = int (TypedValue.ApplyDimension(ComplexUnitType.Dip, 85.0f, this.Activity.Resources.DisplayMetrics))
        let! data = Gravatar.GetImageBytes email px Gravatar.G
        let! image = BitmapFactory.DecodeByteArrayAsync (data, 0, data.Length) |> Async.AwaitTask
        imageView.SetImageDrawable (new CircleDrawable (image)) }

    member private this.CreateLoginView email (inflater:LayoutInflater) container savedInstanceState =
        let view = inflater.Inflate (Resource_Layout.LoginScreen, null)

        imageView <- view.FindViewById<ImageView> (Resource_Id.imageView1)
        this.LoadUserImage email |> Async.StartImmediate

        let textView = view.FindViewById<EditText> (Resource_Id.email)
        textView.Enabled <- false
        textView.Text <- email

        password <- view.FindViewById<EditText> (Resource_Id.password)
        login <- view.FindViewById<Button> (Resource_Id.signInBtn)
        login.Click.Add(fun x-> this.Login email password.Text |> Async.StartImmediate)
        view

    override this.OnCreate savedInstanceState =
        base.OnCreate savedInstanceState
        this.RetainInstance <- true

    override this.OnCreateView (inflater, container, savedInstanceState) =
        match xamarinAccountEmail with
        | None ->
            this.CreateInstructions inflater container savedInstanceState
        | Some email ->
            this.CreateLoginView email inflater container savedInstanceState