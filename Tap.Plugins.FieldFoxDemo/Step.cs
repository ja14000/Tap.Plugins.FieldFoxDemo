// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Keysight.Tap; 

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FF Demo", Groups: new[] { "FieldFoxDemo"}, Description: "Tune in to a radio station, capture data, plot graphs")]

    public class Step : TestStep
    {
        #region Settings

        //Variables

        [Display("Playback Station Frequency", Group: "DUT Setup", Order: 2)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public Enabled <double> StationFrequency { get; set; }

        [Display("Center Frequency", Group: "DUT Setup", Order: 3)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double CenterFrequency { get; set; }

        [Display("Start Frequency", Group: "DUT Setup", Order: 5)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StartFrequency { get; set; }

        [Display("Stop Frequency", Group: "DUT Setup", Order: 4)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public double StopFrequency { get; set; }

        [Display("Amplitude Cut Off", Group: "DUT Setup", Order: 6)]
        [Unit("dBm", UseEngineeringPrefix: true)]
        public int AmplitudeCutOff { get; set; }

        [Display("Points to Sweep", Group: "DUT Setup", Order: 6)]
        public int PointsToSweep { get; set; }

        [Display("Decimal Places To Round Amplitude Values", Group: "DUT Setup", Order: 8)]
        public Enabled <int> RoundTo { get; set; }

        [Display("Channel Span", Group: "DUT Setup", Order: 7)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public Enabled<double> ChannelSpan { get; set; }

        [Display("Look for a specific frequency", Group: "DUT Setup", Order: 1)]
        [Unit("Hz", UseEngineeringPrefix: true)]
        public Enabled <double> MatchFrequency { get; set; }

        //Toggles

        [Display("Freeze Fieldfox Display", Group: "Other Settings", Order: 3)]
        public bool FreezeFF { get; set; }

        [Display("Include GPS Data", Group: "Other Settings", Order: 2)]
        public bool IncludeGPS { get; set; }

        [Display("Preset Instrument", Group: "Other Settings", Order: 1)]
        public bool PresetYesNo { get; set; }

        [Display("Enable Step Verdict", Group: "Other Settings", Order: 5)]
        public bool EnableTestVerdict { get; set; }
        public Verdict MyVerdict;

        //Instrument Declarations

        [Display("FieldFox", Group: "DUT", Order: 1)]
        public FieldFox FF { get; set; }

        #endregion

        public Step()
        {
            // Default Settings

            StationFrequency = new Enabled<double>() { IsEnabled = true, Value = 97000000}; ;
            MatchFrequency = new Enabled<double>() { IsEnabled = false, Value = 88000000};
            CenterFrequency = StationFrequency.Value;
            StartFrequency = 88000000;
            StopFrequency = 108000000;
            ChannelSpan = new Enabled<double>() { IsEnabled = false, Value = 100000}; ;
            AmplitudeCutOff = -80;
            PresetYesNo = false;
            IncludeGPS = true;
            FreezeFF = false;
            EnableTestVerdict = false;
            PointsToSweep = 300;
            RoundTo = new Enabled<int>() { IsEnabled = false, Value = 2 }; 

            //Rules

            Rules.Add(() => (FreezeFF && StationFrequency.IsEnabled) != true, "Freezing the display disables speaker playback", "StationFrequency", "FreezeFF");
            Rules.Add(() => StartFrequency >= 88e6 && StartFrequency <= 108e6, "Start frequency must be greater than 88mhz and less than 108mhz", "StartFrequency");
            Rules.Add(() => CenterFrequency >= 88e6 && CenterFrequency <= 108e6, "Center frequency must be greater than 88mhz and less than 108mhz", "CenterFrequency");
            Rules.Add(() => (StopFrequency > StartFrequency && StopFrequency <= 108e6), "Stop frequency must be greater than the Start Frequency and less than 108mhz", "StopFrequency");
            Rules.Add(() => (MatchFrequency.IsEnabled == true && EnableTestVerdict == true || MatchFrequency.IsEnabled == false && EnableTestVerdict == true), "You must enable the verdict feature to see if a match has been found", "MatchFrequency", "EnableTestVerdict");
            Rules.Add(() => PointsToSweep < 10001, "You cannot sweep more than 10001 points", "PointsToSweep");
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            FF.Preset(PresetYesNo);
            FF.SetPoints(PointsToSweep);
            //Initial array of amplitudes collected by the fieldfox
            var MeasurementResults = FF.GetData(FreezeFF, PointsToSweep, RoundTo.IsEnabled, RoundTo.Value); //Needs to be here because changing the sweep points sometimes does not update everywhere causing a crash when generating the table
        }

        public void SetVerdict()
        {
            MyVerdict = Verdict.NotSet;
        }

        public override void Run()
        {
            //Initial array of amplitudes collected by the fieldfox - Needs to be here again so the variable exists in this context
            var MeasurementResults = FF.GetData(FreezeFF, PointsToSweep, RoundTo.IsEnabled, RoundTo.Value);

            //Pass user defined variables to their respective functions

            FF.RadioMode(StationFrequency.Value, StationFrequency.IsEnabled);
            FF.SAView(CenterFrequency);
            FF.ScanStations(StartFrequency, StopFrequency);

            // Gather GPS Data For Scan
            string GPSDATA = FF.GetGPS();

            // Initial array of frequencies evenly spaced between start and stop value
            var FrequencyList = FF.CalcFrequency(StartFrequency, StopFrequency, PointsToSweep);
            var FrequencyArray = FrequencyList.ToArray();

            //Array of Amplitudes greater than the amplitue cutoff i.e. 'Stations'
            var AmplitudesAboveCutoffArray = FF.AmplitudesAboveCutoff(AmplitudeCutOff, MeasurementResults);
            var StationsFoundList = AmplitudesAboveCutoffArray.ToList();

            // List of frquencies for each 'Station(Amplitude)' value
            var FrequenciesFoundList = FF.FrequenciesAboveCutoff(AmplitudeCutOff, MeasurementResults, FrequencyList);
            var FrequenciesFoundArray = FrequenciesFoundList.ToArray();

            // Sort Frequencies & Amplitudes above cut off into channels
            var PointsToChannels = FF.PointsToChannels(StartFrequency, StopFrequency,ChannelSpan.Value, ChannelSpan.IsEnabled);

            var AmplitudesForChannels = FF.AmplitudesForChannels(PointsToChannels, FrequenciesFoundList, StationsFoundList, ChannelSpan.IsEnabled);

            var FrequenciesForChannels = FF.FrequenciesForChannels(PointsToChannels, FrequenciesFoundList, ChannelSpan.IsEnabled);

            // Results Publishing
            if (IncludeGPS == true)
            {
                Results.PublishTable("Location / Date / Time of Scan: " + GPSDATA, new List<string> { "Frequency(Hz)", "Amplitude(dBm)" }, FrequencyArray, MeasurementResults);
            }
            else
            {
                Results.PublishTable("Channels Frequencies", new List<string> { "Frequency(Hz)", "Amplitude(dBm)" }, FrequencyArray, MeasurementResults);
            }

            Results.PublishTable("Frequencies Above Cutoff", new List<string> { "Station Frequency(Hz)", "Station Amplitude(dBm)" }, FrequenciesFoundArray, AmplitudesAboveCutoffArray);

            if (ChannelSpan.IsEnabled == true)
            {
                Results.PublishTable("Channels", new List<string> { "Station Frequency(Hz)", "Station Amplitude(dBm)" }, FrequenciesForChannels.ToArray(), AmplitudesForChannels.ToArray());
            }

            //Verdict Logic

            if (MatchFrequency.IsEnabled == true && EnableTestVerdict == true)
            {
                var CheckFreq = FF.CheckFreq(FrequenciesFoundList, MatchFrequency.Value);
                if (CheckFreq == true)
                {
                    UpgradeVerdict(Verdict.Pass);
                    //throw new TestPlan.AbortException("Match Frequency Found", true);
                }

                else if (CheckFreq == false)
                {
                    UpgradeVerdict(Verdict.Fail);
                    //throw new TestPlan.AbortException("No Matching Frequency Found", true);
                }
            }

            else if (MatchFrequency.IsEnabled == false && EnableTestVerdict == true)
            {
                
                 if ((EnableTestVerdict == true && AmplitudesAboveCutoffArray.Length != 0))
                {
                    if (FrequencyArray[0] > 0 && MeasurementResults[0] < 0 && FrequenciesFoundArray[0] > 0 && AmplitudesAboveCutoffArray[0] < 0)
                    {
                        UpgradeVerdict(Verdict.Pass);
                       // throw new TestPlan.AbortException("Amplitude Array Not Empty", true);
                    }
                }

                if (AmplitudesAboveCutoffArray.Length == 0)
                {
                    UpgradeVerdict(Verdict.Fail);
                    //throw new TestPlan.AbortException("Amplitude Array Empty", true);
                }            
            }
           
        }
        public override void PostPlanRun()
        {
            base.PostPlanRun();
           


        }
    }

 
}