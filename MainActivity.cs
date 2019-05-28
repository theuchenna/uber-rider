using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Firebase;
using Firebase.Database;
using Android.Views;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android;
using Android.Support.V4.App;
using Android.Content.PM;
using Android.Gms.Location;
using Uber_Rider.Helpers;
using Android.Content;
using Android.Gms.Location.Places.UI;
using Android.Gms.Location.Places;
using Android.Graphics;
using Android.Support.Design.Widget;
using Uber_Rider.EventListeners;
using Uber_Rider.Fragments;
using Uber_Rider.DataModels;
using System;
using Android.Media;

namespace Uber_Rider
{
    [Activity(Label = "@string/app_name", Theme = "@style/UberTheme", MainLauncher = false)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {

        //Firebase
        UserProfileEventListener profileEventListener = new UserProfileEventListener();
        CreateRequestEventListener requestListener;
        FindDriverListener findDriverListener;


        //Views
        Android.Support.V7.Widget.Toolbar mainToolbar;
        Android.Support.V4.Widget.DrawerLayout drawerLayout;

        //TextViews
        TextView pickupLocationText;
        TextView destinationText;
        TextView driverNameText;
        TextView tripStatusText;


        //Buttons
        Button favouritePlacesButton;
        Button locationSetButton;
        Button requestDriverButton;
        RadioButton pickupRadio;
        RadioButton destinationRadio;
        ImageButton callDriverButton;
        ImageButton cancelTripButton;


        //Imageview
        ImageView centerMarker;

        //Layouts
        RelativeLayout layoutPickUp;
        RelativeLayout layoutDestination;

        //Bottomsheets
        BottomSheetBehavior tripDetailsBottonsheetBehavior;
        BottomSheetBehavior driverAssignedBottomSheetBehavior;

        GoogleMap mainMap;

        readonly string[] permissionGroupLocation = { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessCoarseLocation };
        const int requestLocationId = 0;

        LocationRequest mLocationRequest;
        FusedLocationProviderClient locationClient;
        Android.Locations.Location mLastLocation;
        LocationCallbackHelper mLocationCallback;

        static int UPDATE_INTERVAL = 5; //5 SECONDS
        static int FASTEST_INTERVAL = 5;
        static int DISPLACEMENT = 3; //meters

        //Helpers
        MapFunctionHelper mapHelper;

        //TripDetails
        LatLng pickupLocationLatlng;
        LatLng destinationLatLng;
        string pickupAddress;
        string destinationAddress;


        //Flags
        int addressRequest = 1;
        // 1 = Set Address as Pickup Location
        // 2 = Set Address as Destination Location

        // Set address from place search and Ignore Calling FindAddressFromCordinate Method when CameraIdle Event is Fired
        bool takeAddressFromSearch;

        //Fragments
        RequestDriver requestDriverFragment;

        //DataModels
        NewTripDetails newTripDetails;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            ConnectControl();

            SupportMapFragment mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            CheckLocationPermission();
            CreateLocationRequest();
            GetMyLocation();
            StartLocationUpdates();
            profileEventListener.Create();
        }
     
        void ConnectControl()
        {
            //DrawerLayout
            drawerLayout = (Android.Support.V4.Widget.DrawerLayout)FindViewById(Resource.Id.drawerLayout);

            //ToolBar
            mainToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.mainToolbar);
            SetSupportActionBar(mainToolbar);
            SupportActionBar.Title = "";
            Android.Support.V7.App.ActionBar actionBar = SupportActionBar;
            actionBar.SetHomeAsUpIndicator(Resource.Mipmap.ic_menu_action);
            actionBar.SetDisplayHomeAsUpEnabled(true);

            //TextView 
            pickupLocationText = (TextView)FindViewById(Resource.Id.pickupLocationText);
            destinationText = (TextView)FindViewById(Resource.Id.destinationText);
            tripStatusText = (TextView)FindViewById(Resource.Id.tripStatusText);
            driverNameText = (TextView)FindViewById(Resource.Id.driverNameText);

            //Buttons
            favouritePlacesButton = (Button)FindViewById(Resource.Id.favouritePlacesButton);
            locationSetButton = (Button)FindViewById(Resource.Id.locationsSetButton);
            requestDriverButton = (Button)FindViewById(Resource.Id.requestDriverButton);
            pickupRadio = (RadioButton)FindViewById(Resource.Id.pickupRadio);
            destinationRadio = (RadioButton)FindViewById(Resource.Id.DestinationRadio);

            callDriverButton = (ImageButton)FindViewById(Resource.Id.callDriverButton);
            cancelTripButton = (ImageButton)FindViewById(Resource.Id.callDriverButton);

            favouritePlacesButton.Click += FavouritePlacesButton_Click;
            locationSetButton.Click+= LocationSetButton_Click;
            requestDriverButton.Click += RequestDriverButton_Click;
            pickupRadio.Click += PickupRadio_Click;
            destinationRadio.Click += DestinationRadio_Click;

            //Layouts
            layoutPickUp = (RelativeLayout)FindViewById(Resource.Id.layoutPickUp);
            layoutDestination = (RelativeLayout)FindViewById(Resource.Id.layoutDestination);

            layoutPickUp.Click += LayoutPickUp_Click;
            layoutDestination.Click += LayoutDestination_Click;

            //Imageview
            centerMarker = (ImageView)FindViewById(Resource.Id.centerMarker);

            //Bottomsheet
            FrameLayout tripDetailsView = (FrameLayout)FindViewById(Resource.Id.tripdetails_bottomsheet);
            FrameLayout rideInfoSheet = (FrameLayout)FindViewById(Resource.Id.bottom_sheet_trip);

            tripDetailsBottonsheetBehavior = BottomSheetBehavior.From(tripDetailsView);
            driverAssignedBottomSheetBehavior = BottomSheetBehavior.From(rideInfoSheet);

        }


        #region CLICK EVENT HANDLERS

        private void RequestDriverButton_Click(object sender, System.EventArgs e)
        {
            requestDriverFragment = new RequestDriver(mapHelper.EstimateFares());
            requestDriverFragment.Cancelable = false;
            var trans = SupportFragmentManager.BeginTransaction();
            requestDriverFragment.Show(trans, "Request");
            requestDriverFragment.CancelRequest += RequestDriverFragment_CancelRequest;

            newTripDetails = new NewTripDetails();
            newTripDetails.DestinationAddress = destinationAddress;
            newTripDetails.PickupAddress = pickupAddress;
            newTripDetails.DestinationLat = destinationLatLng.Latitude;
            newTripDetails.DestinationLng = destinationLatLng.Longitude;
            newTripDetails.DistanceString = mapHelper.distanceString;
            newTripDetails.DistanceValue = mapHelper.distance;
            newTripDetails.DurationString = mapHelper.durationstring;
            newTripDetails.DurationValue = mapHelper.duration;
            newTripDetails.EstimateFare = mapHelper.EstimateFares();
            newTripDetails.Paymentmethod = "cash";
            newTripDetails.PickupLat = pickupLocationLatlng.Latitude;
            newTripDetails.PickupLng = pickupLocationLatlng.Longitude;
            newTripDetails.Timestamp = DateTime.Now;

            requestListener = new CreateRequestEventListener(newTripDetails);
            requestListener.NoDriverAcceptedRequest += RequestListener_NoDriverAcceptedRequest;
            requestListener.DriverAccepted += RequestListener_DriverAccepted;
            requestListener.TripUpdates += RequestListener_TripUpdates;
            requestListener.CreateRequest();

            findDriverListener = new FindDriverListener(pickupLocationLatlng, newTripDetails.RideID);
            findDriverListener.DriversFound += FindDriverListener_DriversFound;
            findDriverListener.DriverNotFound += FindDriverListener_DriverNotFound;
            findDriverListener.Create();
        }

        void RequestListener_TripUpdates(object sender, CreateRequestEventListener.TripUpdatesEventArgs e)
        {
            if(e.Status == "accepted")
            {
                tripStatusText.Text = "Coming";
                mapHelper.UpdateDriverLocationToPickUp(pickupLocationLatlng, e.DriverLocation);
            }
            else if(e.Status == "arrived")
            {
                tripStatusText.Text = "Driver Arrived";
                mapHelper.UpdateDriverArrived();
                MediaPlayer player = MediaPlayer.Create(this, Resource.Raw.alert);
                player.Start();
            }
            else if (e.Status == "ontrip")
            {
                tripStatusText.Text = "On Trip";
                mapHelper.UpdateLocationToDestination(e.DriverLocation, destinationLatLng);
            }
            else if(e.Status == "ended")
            {
                requestListener.EndTrip();
                requestListener = null;
                TripLocationUnset();

                driverAssignedBottomSheetBehavior.State = BottomSheetBehavior.StateHidden;
                
                MakePaymentFragment makePaymentFragment = new MakePaymentFragment(e.Fares);
                makePaymentFragment.Cancelable = false;
                var trans = SupportFragmentManager.BeginTransaction();
                makePaymentFragment.Show(trans, "payment");
                makePaymentFragment.PaymentCompleted += (i, p) =>
                {
                    makePaymentFragment.Dismiss();
                };
            }
        }


        void RequestListener_DriverAccepted(object sender, CreateRequestEventListener.DriverAcceptedEventArgs e)
        {
            if(requestDriverFragment != null)
            {
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;
            }

            driverNameText.Text = e.acceptedDriver.fullname;
            tripStatusText.Text = "Coming";

            tripDetailsBottonsheetBehavior.State = BottomSheetBehavior.StateHidden;
            driverAssignedBottomSheetBehavior.State = BottomSheetBehavior.StateExpanded;
        }


        void RequestListener_NoDriverAcceptedRequest(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (requestDriverFragment != null && requestListener != null)
                {
                    requestListener.CancelRequestOnTimeout();
                    requestListener = null;
                    requestDriverFragment.Dismiss();
                    requestDriverFragment = null;

                    Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                    alert.SetTitle("Message");
                    alert.SetMessage("Available Drivers Couldn't Accept Your Ride Request, Try again in a few mnoment");
                    alert.Show();
                }
            });

        }


        void FindDriverListener_DriverNotFound(object sender, EventArgs e)
        {
            if(requestDriverFragment != null && requestListener != null)
            {
                requestListener.CancelRequest();
                requestListener = null;
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;

                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Message");
                alert.SetMessage("No available driver found, try again in a few moments");
                alert.Show();
            }
        }


        void FindDriverListener_DriversFound(object sender, FindDriverListener.DriverFoundEventArgs e)
        {
            if(requestListener != null)
            {
                requestListener.NotifyDriver(e.Drivers);
            }
        }


        void RequestDriverFragment_CancelRequest(object sender, EventArgs e)
        {
            //User cancels request before driver accepts it
            if(requestDriverFragment != null && requestListener != null)
            {
                requestListener.CancelRequest();
                requestListener = null;
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;
            }
        }


        async void LocationSetButton_Click(object sender, System.EventArgs e)
        {
            locationSetButton.Text = "Please wait...";
            locationSetButton.Enabled = false;

            string json;
            json = await mapHelper.GetDirectionJsonAsync(pickupLocationLatlng, destinationLatLng);

            if (!string.IsNullOrEmpty(json))
            {
                TextView txtFare = (TextView)FindViewById(Resource.Id.tripEstimateFareText);
                TextView txtTime = (TextView)FindViewById(Resource.Id.newTripTimeText);

                mapHelper.DrawTripOnMap(json);

                // Set Estimate Fares and Time
                txtFare.Text = "$" + mapHelper.EstimateFares().ToString() + " - " + (mapHelper.EstimateFares() + 20).ToString();
                txtTime.Text = mapHelper.durationstring;

                //Display BottomSheet
                tripDetailsBottonsheetBehavior.State = BottomSheetBehavior.StateExpanded;

                //DisableViews
                TripDrawnOnMap();
            }

            locationSetButton.Text = "Done";
            locationSetButton.Enabled = true;

        }

        void FavouritePlacesButton_Click(object sender, System.EventArgs e)
        {

        }


        void PickupRadio_Click(object sender, System.EventArgs e)
        {
            addressRequest = 1;
            pickupRadio.Checked = true;
            destinationRadio.Checked = false;
            takeAddressFromSearch = false;
            centerMarker.SetColorFilter(Color.DarkGreen);

        }

        void DestinationRadio_Click(object sender, System.EventArgs e)
        {
            addressRequest = 2;
            destinationRadio.Checked = true;
            pickupRadio.Checked = false;
            takeAddressFromSearch = false;
            centerMarker.SetColorFilter(Color.Red);

        }


        void LayoutPickUp_Click(object sender, System.EventArgs e)
        {
            AutocompleteFilter filter = new AutocompleteFilter.Builder()
               .SetCountry("NG")
               .Build();

            Intent intent = new PlaceAutocomplete.IntentBuilder(PlaceAutocomplete.ModeOverlay)
                .SetFilter(filter)
                .Build(this);

            StartActivityForResult(intent, 1);
        }

        void LayoutDestination_Click(object sender, System.EventArgs e)
        {
            AutocompleteFilter filter = new AutocompleteFilter.Builder()
                .SetCountry("NG")
                .Build();

            Intent intent = new PlaceAutocomplete.IntentBuilder(PlaceAutocomplete.ModeOverlay)
                .SetFilter(filter)
                .Build(this);

            StartActivityForResult(intent, 2);
        }

        #endregion

        #region MAP AND LOCATION SERVICES

        public void OnMapReady(GoogleMap googleMap)
        {
            mainMap = googleMap;
            mainMap.CameraIdle +=MainMap_CameraIdle;
            string mapkey = Resources.GetString(Resource.String.mapkey);
            mapHelper = new MapFunctionHelper(mapkey, mainMap);
        }

       async void MainMap_CameraIdle(object sender, System.EventArgs e)
        {
            if (!takeAddressFromSearch)
            {
                if (addressRequest == 1)
                {
                    pickupLocationLatlng = mainMap.CameraPosition.Target;
                    pickupAddress = await mapHelper.FindCordinateAddress(pickupLocationLatlng);
                    pickupLocationText.Text = pickupAddress;
                }
                else if (addressRequest == 2)
                {
                    destinationLatLng = mainMap.CameraPosition.Target;
                    destinationAddress = await mapHelper.FindCordinateAddress(destinationLatLng);
                    destinationText.Text = destinationAddress;
                    TripLocationsSet();
                }
            }

        }

        bool CheckLocationPermission()
        {
            bool permissionGranted = false;

            if(ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted &&
                ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted)
            {
                permissionGranted = false;
                RequestPermissions(permissionGroupLocation, requestLocationId);
            }
            else
            {
                permissionGranted = true;
            }

            return permissionGranted;
        }

        void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(UPDATE_INTERVAL);
            mLocationRequest.SetFastestInterval(FASTEST_INTERVAL);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(DISPLACEMENT);
            locationClient = LocationServices.GetFusedLocationProviderClient(this);
            mLocationCallback = new LocationCallbackHelper();
            mLocationCallback.MyLocation += MLocationCallback_MyLocation;

        }

        void MLocationCallback_MyLocation(object sender, LocationCallbackHelper.OnLocationCapturedEventArgs e)
        {
            mLastLocation = e.Location;
            LatLng myposition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
            mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 12));
        }

        void StartLocationUpdates()
        {
            if (CheckLocationPermission())
            {
                locationClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
            }
        }

        void StopLocationUpdates()
        {
            if(locationClient != null && mLocationCallback != null)
            {
                locationClient.RemoveLocationUpdates(mLocationCallback);
            }
        }

        async void GetMyLocation()
        {
            if (!CheckLocationPermission())
            {
                return;
            }

            mLastLocation = await locationClient.GetLastLocationAsync();
            if(mLastLocation != null)
            {
                LatLng myposition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
                mainMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 17));
            }
        }


        #endregion

        #region TRIP CONFIGURATIONS
        void TripLocationsSet()
        {
            favouritePlacesButton.Visibility = ViewStates.Invisible;
            locationSetButton.Visibility = ViewStates.Visible;
        }

        void TripLocationUnset()
        {
            mainMap.Clear();
            layoutPickUp.Clickable = true;
            layoutDestination.Clickable = true;
            pickupRadio.Enabled = true;
            destinationRadio.Enabled = true;
            takeAddressFromSearch = false;
            centerMarker.Visibility = ViewStates.Visible;
            favouritePlacesButton.Visibility = ViewStates.Visible;
            locationSetButton.Visibility = ViewStates.Invisible;
            tripDetailsBottonsheetBehavior.State = BottomSheetBehavior.StateHidden;
            GetMyLocation();
        }


        void TripDrawnOnMap()
        {
            layoutDestination.Clickable = false;
            layoutPickUp.Clickable = false;
            pickupRadio.Enabled = false;
            destinationRadio.Enabled = false;
            takeAddressFromSearch = true;
            centerMarker.Visibility = ViewStates.Invisible;
        }


        #endregion

        #region OVERRIDE METHODS


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(grantResults.Length < 1)
            {
                return;
            }
            if (grantResults[0] == (int)Android.Content.PM.Permission.Granted)
            {
                StartLocationUpdates();
            }
            else
            {

            }
        }


        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(requestCode == 1)
            {
                if(resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;

                    var place = PlaceAutocomplete.GetPlace(this, data);
                    pickupLocationText.Text = place.NameFormatted.ToString();
                    pickupAddress = place.NameFormatted.ToString();
                    pickupLocationLatlng = place.LatLng;
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMarker.SetColorFilter(Color.DarkGreen);
                }
            }

            if (requestCode == 2)
            {
                if (resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;

                    var place = PlaceAutocomplete.GetPlace(this, data);
                    destinationText.Text = place.NameFormatted.ToString();
                    destinationAddress = place.NameFormatted.ToString();
                    destinationLatLng = place.LatLng;
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMarker.SetColorFilter(Color.Red);
                    TripLocationsSet();
                }
            }
        }

        #endregion


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer((int)GravityFlags.Left);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);


            }
        }
    }
}