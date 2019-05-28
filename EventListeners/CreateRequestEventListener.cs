using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Database;
using Java.Util;
using Uber_Rider.DataModels;
using Uber_Rider.Helpers;

namespace Uber_Rider.EventListeners
{
    public class CreateRequestEventListener : Java.Lang.Object, IValueEventListener
    {
        NewTripDetails newTrip;
        FirebaseDatabase database;
        DatabaseReference newTripRef;
        DatabaseReference notifyDriverRef;


        //NotifyDriver
        List<AvailableDriver> mAvailableDrivers;
        AvailableDriver selectedDriver;

        //Timer
        System.Timers.Timer RequestTimer = new System.Timers.Timer();
        int TimerCounter = 0;

        //Flags
        bool isDriverAccepted;

        //Events

        public class DriverAcceptedEventArgs : EventArgs
        {
            public AcceptedDriver acceptedDriver { get; set; }
        }

        public class TripUpdatesEventArgs : EventArgs
        {
            public LatLng DriverLocation { get; set; }
            public string Status { get; set; }
            public double Fares { get; set; }
        }

        public event EventHandler<DriverAcceptedEventArgs> DriverAccepted;
        public event EventHandler NoDriverAcceptedRequest;
        public event EventHandler<TripUpdatesEventArgs> TripUpdates;


        public void OnCancelled(DatabaseError error)
        {
            
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                if(snapshot.Child("driver_id").Value.ToString() != "waiting")
                {
                    string status = "";
                    double fares = 0;

                    if (!isDriverAccepted)
                    {
                        AcceptedDriver acceptedDriver = new AcceptedDriver();
                        acceptedDriver.ID = snapshot.Child("driver_id").Value.ToString();
                        acceptedDriver.fullname = snapshot.Child("driver_name").Value.ToString();
                        acceptedDriver.phone = snapshot.Child("driver_phone").Value.ToString();
                        isDriverAccepted = true;
                        DriverAccepted.Invoke(this, new DriverAcceptedEventArgs { acceptedDriver = acceptedDriver });
                    }

                    //Gets Status
                    if(snapshot.Child("status").Value != null)
                    {
                        status = snapshot.Child("status").Value.ToString();
                    }

                    //Get Fares
                    if(snapshot.Child("fares").Value != null)
                    {
                        fares = double.Parse(snapshot.Child("fares").Value.ToString());
                    }

                    if (isDriverAccepted)
                    {
                        //Get Driver Location Updates
                        double driverLatitude = double.Parse(snapshot.Child("driver_location").Child("latitude").Value.ToString());
                        double driverLongitude = double.Parse(snapshot.Child("driver_location").Child("longitude").Value.ToString());
                        LatLng driverLocationLatLng = new LatLng(driverLatitude, driverLongitude);
                        TripUpdates.Invoke(this, new TripUpdatesEventArgs { DriverLocation = driverLocationLatLng , Status = status, Fares = fares});
                    }
                }
            }
        }

        public CreateRequestEventListener(NewTripDetails mNewTrip)
        {
            newTrip = mNewTrip;
            database = AppDataHelper.GetDatabase();

            RequestTimer.Interval = 1000;
            RequestTimer.Elapsed += RequestTimer_Elapsed;
        }

        public void CreateRequest()
        {
            newTripRef = database.GetReference("rideRequest").Push();

            HashMap location = new HashMap();
            location.Put("latitude", newTrip.PickupLat);
            location.Put("longitude", newTrip.PickupLng);

            HashMap destination = new HashMap();
            destination.Put("latitude", newTrip.DestinationLat);
            destination.Put("longitude", newTrip.DestinationLng);

            HashMap myTrip = new HashMap();

            newTrip.RideID = newTripRef.Key;
            myTrip.Put("location", location);
            myTrip.Put("destination", destination);
            myTrip.Put("destination_address", newTrip.DestinationAddress);
            myTrip.Put("pickup_address", newTrip.PickupAddress);
            myTrip.Put("rider_id", AppDataHelper.GetCurrentUser().Uid);
            myTrip.Put("payment_method", newTrip.Paymentmethod);
            myTrip.Put("created_at", newTrip.Timestamp.ToString());
            myTrip.Put("driver_id", "waiting");
            myTrip.Put("rider_name", AppDataHelper.GetFullName());
            myTrip.Put("rider_phone", AppDataHelper.GetPhone());

            newTripRef.AddValueEventListener(this);
            newTripRef.SetValue(myTrip);
            
        
        }

        public void CancelRequest()
        {
            if(selectedDriver != null)
            {
                DatabaseReference cancelDriverRef = database.GetReference("driversAvailable/" + selectedDriver.ID + "/ride_id");
                cancelDriverRef.SetValue("cancelled");
            }
            newTripRef.RemoveEventListener(this);
            newTripRef.RemoveValue();
        }

        public void CancelRequestOnTimeout()
        {
            newTripRef.RemoveEventListener(this);
            newTripRef.RemoveValue();
        }

        public void NotifyDriver(List<AvailableDriver> availableDrivers)
        {
            mAvailableDrivers = availableDrivers;
            if(mAvailableDrivers.Count >= 1 && mAvailableDrivers != null)
            {
                selectedDriver = mAvailableDrivers[0];
                notifyDriverRef = database.GetReference("driversAvailable/" + selectedDriver.ID + "/ride_id");
                notifyDriverRef.SetValue(newTrip.RideID);

                if(mAvailableDrivers.Count > 1)
                {
                    mAvailableDrivers.RemoveAt(0);
                }
                else if(mAvailableDrivers.Count == 1)
                {
                    //No more available drivers in our list
                    mAvailableDrivers = null;
                }

                RequestTimer.Enabled = true;
            }
            else
            {
                //No driver accepted
                RequestTimer.Enabled = false;
                NoDriverAcceptedRequest?.Invoke(this, new EventArgs());
            }
        }

        void RequestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimerCounter++;

            if(TimerCounter == 10)
            {
                if (!isDriverAccepted)
                {
                    TimerCounter = 0;
                    DatabaseReference cancelDriverRef = database.GetReference("driversAvailable/" + selectedDriver.ID + "/ride_id");
                    cancelDriverRef.SetValue("timeout");

                    if(mAvailableDrivers != null)
                    {
                        NotifyDriver(mAvailableDrivers);
                    }
                    else
                    {
                        RequestTimer.Enabled = false;
                        NoDriverAcceptedRequest?.Invoke(this, new EventArgs());
                    }
                }
               
            }
        }

        public void EndTrip()
        {
            newTripRef.RemoveEventListener(this);
            newTripRef = null;
        }
    }
}