//<copyright>
// Author: MyName
// Copyright:   Copyright 2018 Keysight Technologies
//              You have a royalty-free right to use, modify, reproduce and distribute
//              the sample application files (and/or any modified version) in any way
//              you find useful, provided that you agree that Keysight Technologies has no
//              warranty, obligations or liability for any sample application files.
// </copyright>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Keysight.Tap;

// <template info>
//Note this template assumes that you have a SCPI based instrument, and accordingly
//extends the ScpiInstrument base class.

//If you do NOT have a SCPI based instrument, you should modify this instance to extend
//the (less powerful) Instrument base class.
//</template info>

namespace Tap.Plugins.FieldFoxDemo
{
    [Display("FieldFox", Group: "FieldFoxDemoInstruments", Description: "FieldFox N9917A")]
    [ShortName("FieldFox")]
    public class FieldFox : ScpiInstrument
    {
        #region Settings
        // ToDo: Add property here for each parameter the end user should be able to change
        #endregion
        public FieldFox()
        {
            //ScpiCommand("SYSTem:PRESet");
            // ScpiQuery("*OPC?");
            //ScpiCommand(@"INSTrument:SELect ""SA""");
        }

        public override void Open()
        {
            base.Open();
            if (!IdnString.Contains("N9"))
            {
                Log.Error("This instrument driver does not support the connected instrument.");
                throw new ArgumentException("Wrong instrument type.");
            }
        }

        public override void Close()
        {
            // TODO:  Shut down the connection to the instrument here.
            base.Close();
        }

        public void PrePlanSetup(int PointsToSweep)
        {
            ScpiCommand(@"INSTrument:SELect ""SA""");
            ScpiQuery("*OPC?");
            ScpiCommand("SWE:POIN " + PointsToSweep);
        }

        //<summary>
        //Select the Spectrum Analyzer Instrument
        //Activate FM Wide listening mode
        //Tune to radio station
        //Turn On the Pre-Amp
        //</summary>
        public void Preset(bool PresetYesNo)
        {
            if(PresetYesNo == true)
            {
                ScpiCommand("SYSTem:PRESet");
                ScpiCommand("*OPC");
                ScpiQuery("*OPC?");
            }
        }

        //<summary>
        //Select the Spectrum Analyzer Instrument
        //Activate FM Wide listening mode if user has selected
        //Tune to radio station if user has selected
        //Turn On the Pre-Amp
        //</summary>
        public void SetListen(double StationFrequency, bool PlayYesNo)
        {
            //ScpiCommand(@"INSTrument:SELect ""SA"""); 

            if(PlayYesNo == true)
            {
                ScpiCommand("TAL:DST 1");
                ScpiCommand("SENSe:MEASurement:TAListen FMW");
                ScpiCommand(":SENSe:TAListen:TFReq " + StationFrequency);    
            }

            if (PlayYesNo == false)
            {  
                ScpiCommand("TAL:DST 0");
            }

            ScpiCommand(":SENSe:POWer:RF:GAIN:STATe 1");
        }

        //<summary>
        //Set the center frequency to the value held in the CenterFrequecy
        //<summary>
        public void SetDisplay(double CenterFrequency)
        {
            ScpiCommand(":SENSe:FREQuency:CENTer " + CenterFrequency);
            ScpiCommand("DISPlay:WINDow:TRACe:Y:SCALe:AUTO");
        }

        //<summary>
        //Set Start Frequency
        //Set StopFrequency
        //<summary>
        public void SetFrequencies(double StartFrequency, double StopFrequency)
        {
            ScpiCommand(":SENS:FREQ:STAR " + StartFrequency); 
            ScpiCommand(":SENS:FREQ:STOP " + StopFrequency); 
        }

        public double[] GetData(bool FreezeFF, int PointsToSweep)
        {
          
            ScpiCommand("TRAC1:TYPE AVG");
            ScpiCommand("DISPlay:WINDow:TRACe:Y:SCALe:AUTO");
            //ScpiCommand("*OPC");
            //ScpiQuery("*OPC?");

            if (FreezeFF == true)
            {
                ScpiCommand("INITiate:CONTinuous 0" );
                //ScpiCommand("*OPC");
               //ScpiQuery("*OPC?");
            }
            else
            {
                ScpiCommand("INITiate:CONTinuous 1");
                //ScpiCommand("*OPC");
                //ScpiQuery("*OPC?");
            }
            
            //ScpiQuery("*OPC?");
            var data = ScpiQuery<double[]>("TRAC1:DATA?");
            ScpiQuery("*OPC?");

            return data;
        }

        public string GetGPS()
        {
            var storage = ScpiQuery("SYSTem:GPS:DATA?");
            storage = storage.Replace("\"", "");
            return storage; 
        }

        //<summary>
        // This function generates a list of frequencies based on the start frequency, stop frequency and number of points that are
        // displayed on the FieldFox. These frequency values are used on the x-axis of the 'FM Spectrum View' plot and for later operations.
        //</summary>
        public List<double> GenerateFrequencies(double StartFrequency, double StopFrequency, int PointsToSweep)
        {
            var FrequencyStep = ((StopFrequency - StartFrequency) / (PointsToSweep));
            List<double> FrequencyList = new List<double>();
            FrequencyList.Add(StartFrequency);

            for (int runs = 0; runs < (PointsToSweep-1); runs++)
            {
                FrequencyList.Add(FrequencyList[runs] + FrequencyStep);
            }
            return FrequencyList; 
        }

        //<summary>
        // Returns a list of Amplitudes from the MeasurementResults array that have a value greater than the 'AmplitudeCutoff' variable
        //</summary>
        public double[] AmplitudesAboveCutoff(int AmplitudeCutOff, double[] MeasurementResults)
        {
            var AmplitudesAboveCutoff = MeasurementResults.Where(item => item >= AmplitudeCutOff).ToArray();
            return AmplitudesAboveCutoff; 
        }

        public List<double> FrequenciesAboveCutoff(double[] AmplitudesAboveCutOff, List<double> FrequencyList, List<double> MeasurementResults)
        {
            List<double> FrequenciesAboveCutoff = new List<double>();
            foreach (double i in AmplitudesAboveCutOff)
            {
               FrequenciesAboveCutoff.Add(FrequencyList[MeasurementResults.IndexOf(i)]);   
            }
            return FrequenciesAboveCutoff;
        }

        //<summary>
        // This function checks if the value chosen by the user (CheckFreq) is in the FreqienciesFoundList. If it is it returs 'true' and the validation shows a 'pass' for
        //'false' the validation shows 'fail'.
        //</summary>
        public bool? FrequencyMatch(List<double> FrequenciesFoundList, double MatchFrequency)
        {
            bool? MatchFound = null;
            foreach (double i in FrequenciesFoundList)
            {
                
                if (FrequenciesFoundList.Contains(MatchFrequency))
                {
                    MatchFound = true;
                    break;
                }
                else
                {
                    MatchFound = false;
                }

            }
            return MatchFound;
        }

        //<summary>
        //This function returns a list of channels between the start and stop frequencies the spacing of with is defined by the ChannelSpan value.
        //Depending on the ChannelSpan value chosen by the user either only odd channels or only even channels would be created so this 
        //function adds 100000 to the start frequency to ensure both the even and odd channels are created. 
        //<summary>
        public List<Double> GenerateChannels(double StartFrequency, double StopFrequency, double ChannelSpan, bool Enabled)
        {
            double StartFrequencyOdd = StartFrequency + 100000;
            double StopFrequencyOdd = StopFrequency + 100000;
            double ChannelStart = StartFrequency;
            double ChannelStartOdd = StartFrequency + 100000;

            List<double> ChannelList = new List<double>();
            if (Enabled == true)
            {
                while (StopFrequency >= StartFrequency)
                {
                    ChannelList.Add(ChannelStart);
                    ChannelStart = StartFrequency += ChannelSpan;

                    ChannelList.Add(ChannelStartOdd);
                    ChannelStartOdd = StartFrequencyOdd += ChannelSpan;                                      
                }
            }
            return ChannelList;
        }

        //<summary>
        // The Channels created in CalcChannels will always be integer values e.g. 88000000MHz since the the frequencies generated by the CalcFrequency 
        // fuction may not always be integer values they must be rounded to the nearest million before checking if it fits into a channel. Once the frequency value has been rounded this function
        // checks if a matching value exists in the ChannelList list. E.g. 88066666.6666667 gets rounded to 88066666 then divided by 1000000 to 
        //88.066666 then it is rounded again to 88.0 and finally multiplied by 1000000 to get back the final rounded frequency. Then the function checks if a channel
        //matching this rounded value exists in the channel list, if it does it's correspondinding amplitude is added to the ChannelAmplitudeList.
        //<summary>
        public List<Double> AmplitudesForChannels(List<double> ChannelList, List<double> FrequenciesAboveCutOff, List<double> AmplitudesAboveCutOff, bool Enabled)
        {

            List<double> ChannelAmplitudeList = new List<double>();
            int x = 0;
            foreach (double i in FrequenciesAboveCutOff)
            {
                var a = Math.Round(i, 0);
                var z = a / 1000000; // this might fuck things up at lower frequeicies
                var b = Math.Round(z, 1);
                var c = b * 1000000;

                if (ChannelList.Contains(c))
                {
                    ChannelAmplitudeList.Add(AmplitudesAboveCutOff[FrequenciesAboveCutOff.IndexOf(i)]);
                    x++;
                }           
            }

            return ChannelAmplitudeList;
        }

        //<summary>
        // The Channels created in CalcChannels will always be integer values e.g. 88000000MHz since the the frequencies generated by the CalcFrequency 
        // fuction may not always be integer values they must be rounded to the nearest million before checking if it fits into a channel. Once the frequency value has been rounded this function
        // checks if a matching value exists in the ChannelList list. E.g. 88066666.6666667 gets rounded to 88066666 then divided by 1000000 to 
        //88.066666 then it is rounded again to 88.0 and finally multiplied by 1000000 to get back the final rounded frequency. Then the function checks if a channel
        //matching this rounded value exists in the channel list, if it does it is added to the ChannelFrequencyList.
        //<summary>
        public List<Double> FrequenciesForChannels(List<double> ChannelList, List<double> FrequenciesAboveCutOff, bool Enabled)
        {
            List<double> ChannelFrequencyList = new List<double>();
            int x = 0;
            foreach (double i in FrequenciesAboveCutOff)
            {
                var a = Math.Round(i, 0);
                var z = a / 1000000;
                var b = Math.Round(z, 1);
                var c = b * 1000000;

                if (ChannelList.Contains(c))
                {
                    ChannelFrequencyList.Add(c);
                    x++;
                }               
            }
            return ChannelFrequencyList;
        }

        
    }

}


