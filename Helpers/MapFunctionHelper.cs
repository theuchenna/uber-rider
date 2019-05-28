using System;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Com.Google.Maps.Android;
using Java.Util;
using Newtonsoft.Json;
using ufinix.Helpers;
using yucee.Helpers;

namespace Uber_Rider.Helpers
{
    public class MapFunctionHelper
    {
        string mapkey;
        GoogleMap map;
        public double distance;
        public double duration;
        public string distanceString;
        public string durationstring;
        Marker pickupMarker;
        Marker driverLocationMarker;
        bool isRequestingDirection;

        public MapFunctionHelper(string mMapkey, GoogleMap mmap)
        {
            mapkey = mMapkey;
            map = mmap;
        }

        public string GetGeoCodeUrl( double lat, double lng)
        {
            string url = "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + lat + "," + lng + "&key=" + mapkey;
            return url;
        }

        public async Task<string> GetGeoJsonAsync(string url)
        {
            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            string result = await client.GetStringAsync(url);
            return result;
        }

        public async Task<string> FindCordinateAddress(LatLng position)
        {
            string url = GetGeoCodeUrl(position.Latitude, position.Longitude);
            string json = "";
            string placeAddress = "";

            //Check for Internet connection
            json = await GetGeoJsonAsync(url);

            if (!string.IsNullOrEmpty(json))
            {
                var geoCodeData = JsonConvert.DeserializeObject<GeocodingParser>(json);
                if (!geoCodeData.status.Contains("ZERO"))
                {
                    if(geoCodeData.results[0] != null)
                    {
                        placeAddress = geoCodeData.results[0].formatted_address;
                    }
                }
            }

            return placeAddress;
        }

        public async Task<string> GetDirectionJsonAsync(LatLng location, LatLng destination)
        {
            //Origin of route
            string str_origin = "origin=" + location.Latitude + "," + location.Longitude;

            //Destination of route
            string str_destination = "destination=" + destination.Latitude + "," + destination.Longitude;

            //mode
            string mode = "mode=driving";

            //Buidling the parameters to the webservice
            string parameters = str_origin + "&" + str_destination + "&" + "&" + mode +  "&key=";

            //Output format
            string output = "json";

            string key = mapkey;

            //Building the final url string
            string url = "https://maps.googleapis.com/maps/api/directions/" + output + "?" + parameters + key;

            string json = "";
            json = await GetGeoJsonAsync(url);

            return json;
            
        }

        public void DrawTripOnMap(string json)
        {
            var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);

            //Decode Encoded Route
            var points = directionData.routes[0].overview_polyline.points;
            var line = PolyUtil.Decode(points);

            ArrayList routeList = new ArrayList();
            foreach (LatLng item in line)
            {
                routeList.Add(item);
            }

            //Draw Polylines on Map
            PolylineOptions polylineOptions = new PolylineOptions()
                .AddAll(routeList)
                .InvokeWidth(10)
                .InvokeColor(Color.Teal)
                .InvokeStartCap(new SquareCap())
                .InvokeEndCap(new SquareCap())
                .InvokeJointType(JointType.Round)
                .Geodesic(true);

            Android.Gms.Maps.Model.Polyline mPolyline = map.AddPolyline(polylineOptions);

            //Get first point and lastpoint
            LatLng firstpoint = line[0];
            LatLng lastpoint = line[line.Count - 1];

            //Pickup marker options
            MarkerOptions pickupMarkerOptions = new MarkerOptions();
            pickupMarkerOptions.SetPosition(firstpoint);
            pickupMarkerOptions.SetTitle("Pickup Location");
            pickupMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));

            //Destination marker options
            MarkerOptions destinationMarkerOptions = new MarkerOptions();
            destinationMarkerOptions.SetPosition(lastpoint);
            destinationMarkerOptions.SetTitle("Destination");
            destinationMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));


            MarkerOptions driverMarkerOptions = new MarkerOptions();
            driverMarkerOptions.SetPosition(firstpoint);
            driverMarkerOptions.SetTitle("Current Location");
            driverMarkerOptions.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.position));
            driverMarkerOptions.Visible(false);


            pickupMarker = map.AddMarker(pickupMarkerOptions);
            Marker destinationMarker = map.AddMarker(destinationMarkerOptions);
            driverLocationMarker = map.AddMarker(driverMarkerOptions);

            //Get Trip Bounds
            double southlng = directionData.routes[0].bounds.southwest.lng;
            double southlat = directionData.routes[0].bounds.southwest.lat;
            double northlng = directionData.routes[0].bounds.northeast.lng;
            double northlat = directionData.routes[0].bounds.northeast.lat;

            LatLng southwest = new LatLng(southlat, southlng);
            LatLng northeast = new LatLng(northlat, northlng);
            LatLngBounds tripBound = new LatLngBounds(southwest, northeast);

            map.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(tripBound, 100));
            map.SetPadding(40, 40, 40, 40);
            pickupMarker.ShowInfoWindow();

            duration = directionData.routes[0].legs[0].duration.value;
            distance = directionData.routes[0].legs[0].distance.value;
            durationstring = directionData.routes[0].legs[0].duration.text;
            distanceString = directionData.routes[0].legs[0].distance.text;

        }

        public double EstimateFares()
        {
            double basefare = 20; //USD 
            double distanceFare = 5; //USD per kilometer
            double timefare = 3; //USD per minute

            double kmfares = (distance / 1000) * distanceFare;
            double minsfares = (duration / 60) * timefare;

            double amount = kmfares + minsfares + basefare;
            double fares = Math.Floor(amount / 10) * 10;

            return fares;

        }

        public async void UpdateDriverLocationToPickUp(LatLng firstposition, LatLng secondposition)
        {
            if (!isRequestingDirection)
            {
                isRequestingDirection = true;
                string json = await GetDirectionJsonAsync(firstposition, secondposition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                pickupMarker.Title = "Pickup Location";
                pickupMarker.Snippet = "Your Driver is " + duration + " Away";
                pickupMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }
           

        }

        public void UpdateDriverArrived()
        {
            pickupMarker.Title = "Pickup Location";
            pickupMarker.Snippet = "Your Driver has Arrived";
            pickupMarker.ShowInfoWindow();
        }

        public async void UpdateLocationToDestination(LatLng firstposition, LatLng secondposition)
        {
            driverLocationMarker.Visible = true;
            driverLocationMarker.Position = firstposition;
            map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(firstposition, 15));

            if (!isRequestingDirection)
            {
                //Check Connection
                isRequestingDirection = true;
                string json = await GetDirectionJsonAsync(firstposition, secondposition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                driverLocationMarker.Title = "Current Location";
                driverLocationMarker.Snippet = "Your Destination is " + duration + " Away";
                driverLocationMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }

        }
    }
}
