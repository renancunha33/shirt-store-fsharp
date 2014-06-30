namespace XamarinStore

open System
open System.Linq
open System.Drawing
open System.Collections.Generic

open MonoTouch.UIKit
open MonoTouch.Foundation
open MonoTouch.CoreAnimation
open MonoTouch.CoreGraphics
open FileCache
open Product

[<Register ("AppDelegate")>]
type AppDelegate () =
    inherit UIApplicationDelegate ()

    let mutable window = null
    let mutable navigation:UINavigationController = null
    let mutable button:BasketButton = null

    let createBasketButton () =
        button.ItemsCount <- WebService.CurrentOrder.Products.Length
        new UIBarButtonItem(button)

    let orderCompleted () =
        navigation.PopToRootViewController true |> ignore

    let proccessOrder() =
        let processing = new ProcessingViewController (WebService.Shared.CurrentUser, OrderPlaced = orderCompleted)
        navigation.PresentViewController (new UINavigationController(processing), true, null)

    let showAddress () =
        let addreesVc = new ShippingAddressViewController (WebService.Shared.CurrentUser, ShippingComplete = proccessOrder)
        navigation.PushViewController (addreesVc, true)

    let showLogin () =
        let loginVc = new LoginViewController (LoginSucceeded = showAddress)
        navigation.PushViewController (loginVc, true)

    let showBasket () =
        let basketVc = new BasketViewController(Checkout = showLogin)
        navigation.PushViewController (basketVc, true)
    
    let updateProductsCount() =
        button.UpdateItemsCount WebService.CurrentOrder.Products.Length

    let showProductDetail (product: Product) =
        let productDetails = new ProductDetailViewController(product, createBasketButton)
        productDetails.AddToBasket <- fun product->
                                        WebService.CurrentOrder <- { WebService.CurrentOrder with Products = product::WebService.CurrentOrder.Products }
                                        updateProductsCount()

        navigation.PushViewController (productDetails, true)

    // This method is invoked when the application is ready to run.
    override this.FinishedLaunching (app, options) =

        window <- new UIWindow (UIScreen.MainScreen.Bounds)
        button <- new BasketButton(Frame = new RectangleF(0.0f, 0.0f, 44.0f, 44.0f))
        button.TouchUpInside.Add(fun _ -> showBasket())

        FileCache.saveLocation <- System.IO.Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Personal)).ToString () + "/tmp"

        UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false)

        UINavigationBar.Appearance.SetTitleTextAttributes(new UITextAttributes (TextColor = UIColor.White))

        let productVc = new ProductListViewController (createBasketButton)
        productVc.ProductTapped <-fun product-> showProductDetail product
        navigation <- new UINavigationController (productVc)

        navigation.NavigationBar.TintColor <- UIColor.White
        navigation.NavigationBar.BarTintColor <- Color.Blue.ToUIColor()

        window.RootViewController <- navigation
        
        window.MakeKeyAndVisible ()
        true

module Main =
    [<EntryPoint>]
    let main args =
        UIApplication.Main (args, null, "AppDelegate")
        0

