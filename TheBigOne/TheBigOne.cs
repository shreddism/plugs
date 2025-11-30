/* THE SOWOFTWARE IS PROWOVIDED “AS IS”, WITHOWOUWUT WARRANTY OWOF ANY KIND, EXPRESS OR IMPLIED, INCLUWUDING BUWUT NOWOT LIMITED TOWO THE WARRANTIES OWOF MERCHANTABILITY, FITNESS FOWOR A PARTICUWULAR PUWURPOWOSE AND NOWONINFRINGEMENT.
 * a plugin 5/10/2025
 * feedback and stuwuff welcome
 * made by jaaakb (owosuwu)/jaaakb (discoword)
 */


using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;
using OpenTabletDriver.Plugin.Timing;
using System.Numerics;


namespace QuantumDotNetIntangibleBlockchainDotComArtificialIntelligenceMachineLearning
{
    [PluginName("The End")]
    public class CircleEmulatorInterfaceZero : AsyncPositionedPipelineElement<IDeviceReport>
    {
        public CircleEmulatorInterfaceZero() : base()
        {
        }

        [Property("Duplicate report fix"), DefaultPropertyValue(false), ToolTip
        (
        "Ignore extra duplicate reports, maybe when pen detects pressure or when a key is pressed.\n" +
        "May help on some new wacom tablets or other brand tablets. If it removes buggy cursor glitching (when pressing a button), it's working.\n"
        )]
        public bool GetDoubleReportFix
        {
            get { return _doubleReportFix; }
            set { _doubleReportFix = value; }
        }

        [Property("smooth transition time"), DefaultPropertyValue(1f), ToolTip
        (
        "Prediction transition/deviation interpolation/smoothing. 0 ~ 1 or maybe above, but can add delay.\n" +
        "should be just good, but if you want more 'raw' output, set closer to or to zero (off)\n"
        )]
        public float GetSmoothTransition
        {
            get { return _smoothTransition; }
            set { _smoothTransition = value; }
        }


        [Property("TimeOffset"), DefaultPropertyValue(0.75f), ToolTip
        (
        "Same scheme as before/temp resampler. 0 = no prediction, 1 = predict 1 report ahead\n" +
        "It may work best when set to 0.6 ~ 1.1, because it trains based on predicting next report (1 ahead)\n" +
        "The higher it is (predicting future ahrd), the more bad it is, so it also works the better the lower it is."
        )]
        public float GetOffset
        {
            get { return _timeOffset; }
            set { _timeOffset = value - 1f; }
        }

        [Property("Multiplier weights"), DefaultPropertyValue(100), ToolTip
        (
        "How many differend weights are tested per derivative\n" +
        "50 is probably good enough, but the cost of this seems very low. 100-1000 might also be fine on most pcs.\n" +
        "Very diminishing gains maybe, but i'm not sure. Check daemon to see if calc is overloaded (values over 0.5 or 0.75 is alarming)\n" +
        "Higher = linear calculation cost increase\n"
        )]
        public int GetWeights
        {
            get { return _weights; }
            set { _weights = value; }
        }

        [Property("derivatives"), DefaultPropertyValue(5), ToolTip
        (
        "1+. 2 - 3 is probably most gains you see, 4 might be slight difference, 5 i doubt will ever be useful (maybe slightly on 300hz tablet)\n" +
        "Higher = linear calculation cost increase\n"
        )]
        public int GetDerivatives
        {
            get { return _derivatives; }
            set { _derivatives = value; }
        }

        [Property("_categories"), DefaultPropertyValue(96), ToolTip
        (
        "Not rly so impactful, needs other value to function. Affects max change of value to train for discretely, \n" +
        "lower value caps the change and treats all changes above it as one category. Default 64 but u can just use 46 (M) or 33 (S) if area is small\n" +
        "96 is sort of an overkill, but there shouldn't be big downsides."
        )]
        public int GetCategories
        {
            get { return _categories; }
            set { _categories = value; }
        }

        [Property("GetTabletHz"), DefaultPropertyValue(133f), ToolTip
        (
        "GetTabletHz \n" +
        "\n"
        )]
        public float GetTabletHz
        {
            get { return _tabletHz; }
            set { _tabletHz = value; }
        }

        [Property(" hzduplicate"), DefaultPropertyValue(0), ToolTip
        (
        "0 off 1 +100%duping, 2 = +200%duping \n" +
        "\n"
        )]
        public int GetDuplicateHz
        {
            get { return _duplicate; }
            set { _duplicate = value; }
        }


        private HPETDeltaStopwatch _reportWatch = new HPETDeltaStopwatch(startRunning: false);

        private bool _doubleReportFix;

        private int _duplicate;
        private bool _initialise = true;
        private bool _stateReset = true;

        private int _stateUpdates;

        private Vector2[,] _rawD;
        private Vector2[,] _bestD;
        private int[,] _rawDDCat;
        private int[,] _rawDMCat;

        private int[,] _dCategoryStateWeight;
        private int[,] _mCategoryStateWeight;

        private int[] _t;
        private float[] _weight;

        private uint _pressure;
        private Vector2 _rawPosition;

        private Vector2 _outputPosition;

        private int _weights;// = 32;
        private int _derivatives;// = 5;
        private int _categories;// = 64;
        private float _timeOffset;// = 0f;
        private float _smoothTransition;// = true;
        private float _tabletHz;// = 200f;


        private float _weightMulti;// = 0.95f;

        private int _tSize = 4;

        private float _ignoreDuplicateVelocity = 100f;
        private bool _ignoreDuplicate = true;
        private bool _interpolation = true;

        private float _recentDelta;
        private float _reportDelta;
        private float _leftoverDelta;
        private float _leftoverDeltaMin;
        private float _leftoverDeltaMax;

        private float _interpLeftOver;
        private float _interpTime;
        private float _interpSpeedMulti;

        private bool _runCalc = false;

        private float _statCalcTime = 0;
        private float _statAverageProgress = 0;
        private float _statPathDeviation = 0;

        private float[,,] _dWeightError;
        private float[,,] _mWeightError;


        private Vector2 _smoothTransitionVector;


        private void FilterWeightInitialisation() //super simple for now
        {
            //double WeightMulti = 1f - 0.005f * (1000f / _weights); //Will create weights with a minimum of ~0.006f
            //double[] TempWeight = new double[_weights];

            //_weight[0] = 0f;

            //_weight[1] = 1f;
            //TempWeight[1] = 1d;



            for (int w = 0; w < _weights; w++) 
            {
                //TempWeight[w] = WeightMulti * TempWeight[w - 1];
                _weight[w] = (float)((double)(w)/(double)(_weights - 1));
            }
        }

        private Vector2 FilterInterpolate(float Time)
        {
            Vector2 TempVector = _bestD[_t[0], 0];

            if (Time < _smoothTransition * (_reportDelta + _leftoverDeltaMax))
                TempVector += _smoothTransitionVector * (1f - Time / (_smoothTransition * (_reportDelta + _leftoverDeltaMax)));

            float TimeMulti = _timeOffset + (_interpLeftOver + Time * _interpSpeedMulti) / _reportDelta;
            Time = TimeMulti;

            if (TimeMulti < 0)
                TimeMulti *= -1f;

            float[] DampeningWeight = new float[_derivatives];

            DampeningWeight[_derivatives - 1] = _weight[_mCategoryStateWeight[_rawDMCat[_t[1], _derivatives - 1], _derivatives - 1]];
            if(_derivatives > 1)
                for (int d = _derivatives - 1; d < 0; d--)
                {
                    DampeningWeight[d - 1] = _weight[_mCategoryStateWeight[_rawDMCat[_t[1], d - 1], d - 1]];
                    if (DampeningWeight[d - 1] < DampeningWeight[d])
                        DampeningWeight[d - 1] = DampeningWeight[d];
                }

            for (int d = 1; d < _derivatives; d++)
            {
                TempVector += Time * _bestD[_t[0], d] * DampeningWeight[d];
                Time *= TimeMulti;

                //if (_weight[_dCategoryStateWeight[_rawDDCat[_t[0], d], d]] == 0f)
                //    d = _derivatives; //break 
            }

            return TempVector;
        }

        private void FilterStateUpdatePrediction()
        {
            float[,] RawError = new float[_derivatives, _weights];
            //float[,] MagError = new float[_derivatives, _weights];

            //float BestWeight;

            Vector2 TempVector = Vector2.Zero;

            for (int d = 0; d < _derivatives; d++)
            {
                float BestError = System.Single.MaxValue;
                float BestWeight = _dCategoryStateWeight[_rawDDCat[_t[1], d], d];


                RawError[d, 0] = (_rawD[_t[0], 0] - TempVector).Length();

                for (int w = 1; w < _weights; w++)
                {
                    RawError[d, w] = (_rawD[_t[0], 0] - (TempVector + _rawD[_t[1], d] * _weight[w] + _bestD[_t[2], d] * (1f - _weight[w]))).Length();
                }

                for (int w = 1; w < _weights; w++)
                {
                    if (d != 0)
                        _dWeightError[d, w, _rawDDCat[_t[1], d]] = (RawError[d, w] - RawError[d - 1, _dCategoryStateWeight[_rawDDCat[_t[1], d - 1], d - 1]]) + 0.9999f * _dWeightError[d, w, _rawDDCat[_t[1], d]];
                    else
                        _dWeightError[0, w, _rawDDCat[_t[1], 0]] = RawError[0, w] + 0.9999f * _dWeightError[0, w, _rawDDCat[_t[1], 0]];

                    // !! ABOVE IS FASTER BUT THE NUMBERS SAVED ARE UGLY
                    //if(d!=0)
                    //    _dWeightError[d, w, _rawDDCat[_t[1], d]] = (RawError[d, w] - RawError[d - 1, _categoryStateWeight[_rawDDCat[_t[1], d - 1], d - 1]]) * 0.0001f + 0.9999f * _dWeightError[d, w, _rawDDCat[_t[1], d]];
                    //else
                    //    _dWeightError[d, w, _rawDDCat[_t[1], d]] = RawError[d, w] * 0.0001f + 0.9999f * _dWeightError[d, w, _rawDDCat[_t[1], d]];

                    if (_dWeightError[d, w, _rawDDCat[_t[1], d]] < BestError)
                    {
                        BestError = _dWeightError[d, w, _rawDDCat[_t[1], d]];
                        _dCategoryStateWeight[_rawDDCat[_t[1], d], d] = w;
                        BestWeight = _weight[w];
                    }
                }

                //if(BestWeight != 0f)
                TempVector += _rawD[_t[1], d] * BestWeight + _bestD[_t[2], d] * (1f - BestWeight);
            }


            //Magnitude dampening weight thiing below ************************
            int DMax = _derivatives - 1;

            float[,] MagError = new float[_derivatives, _weights];

            //Vector2 BestVector = Vector2.Zero;
            TempVector = Vector2.Zero;

            for (int d = 0; d < _derivatives; d++)
            {
                TempVector += _bestD[_t[1], d]; 
            }

            for (int d = DMax; d > 0; d--) //We do not do this 'dampening' on position, that would be predicting the area your cursor is confined in. (do not look at d == 0)
            {
                float BestError = System.Single.MaxValue; //(_rawD[_t[0], 0] - BestVector).Length();
                int Category = _rawDMCat[_t[1], d];
                float BestWeight = _weight[_mCategoryStateWeight[Category, d]];

                for (int w = 0; w < _weights; w++)
                {
                    MagError[d, w] = (_rawD[_t[0], 0] - (TempVector - _bestD[_t[1], d] * (1f - _weight[w]))).Length();
                    _mWeightError[d, w, Category] = MagError[d, w] - RawError[d - 1, _dCategoryStateWeight[_rawDDCat[_t[1], d - 1], d - 1]] + 0.99999f * _mWeightError[d, w, Category];

                    if (_mWeightError[d, w, Category] < BestError)
                    { 
                        _mCategoryStateWeight[Category, d] = w;
                        BestError = _mWeightError[d, w, Category];
                        BestWeight = _weight[w];
                    }
                }

                TempVector -= _bestD[_t[1], d];
            }
        }

        private void FilterNormaliseError()
        {
            float Weight1 = 0.1f;
            float Weight2 = 1f - Weight1;
            float Weigth3 = 1f - 2 * Weight1;

            for (int d = 0; d < _derivatives; d++)
                for (int w = 0; w < _weights; w++)
                {
                    _mWeightError[d, w, 0] = Weight1 * _mWeightError[d, w, 1] + Weight2 * _mWeightError[d, w, 0];
                    _mWeightError[d, w, _categories - 1] = Weight1 * _mWeightError[d, w, _categories - 1] + Weight2 * _mWeightError[d, w, _categories - 2];
                    _dWeightError[d, w, 0] = Weight1 * _dWeightError[d, w, 1] + Weight2 * _dWeightError[d, w, 0];
                    _dWeightError[d, w, _categories - 1] = Weight1 * _dWeightError[d, w, _categories - 1] + Weight2 * _dWeightError[d, w, _categories - 2];
                }

            for (int d = 0; d < _derivatives; d++)
                for (int w = 0; w < _weights; w++)
                    for (int c = 1; c < _categories - 1; c++)
                    {
                        _mWeightError[d, w, c] = Weight1 * (_mWeightError[d, w, c + 1] + _mWeightError[d, w, c - 1]) + Weigth3 * _mWeightError[d, w, c];
                        _dWeightError[d, w, c] = Weight1 * (_dWeightError[d, w, c + 1] + _dWeightError[d, w, c - 1]) + Weigth3 * _dWeightError[d, w, c];
                    }
        }

        private void FilterInitialise()
        {
            _stateReset = true;

            //Initialise all arrays and stuff here

            _rawD = new Vector2[_tSize, _derivatives + 1];
            _bestD = new Vector2[_tSize, _derivatives + 1];
            _rawDDCat = new int[_tSize, _derivatives];
            _rawDMCat = new int[_tSize, _derivatives];

            _t = new int[_tSize];
            _weight = new float[_weights];

            _dWeightError = new float[_derivatives, _weights, _categories];
            _mWeightError = new float[_derivatives, _weights, _categories];

            _dCategoryStateWeight = new int[_categories, _derivatives];
            _mCategoryStateWeight = new int[_categories, _derivatives];


            for (int i = 0; i < _categories; i++)
            {
                _dCategoryStateWeight[i, 0] = 1;
                _mCategoryStateWeight[i, 0] = 1;
            }
            
            //***

            //Initialse timing


            _reportWatch.Restart();

            _reportDelta = 1000f / _tabletHz;

            _recentDelta = _reportDelta;

            _interpTime = 0f;

            _leftoverDelta = 0f;

            _interpSpeedMulti = 1f;

            _interpLeftOver = 0f;

            _leftoverDeltaMax = 1f;
            _leftoverDeltaMin = -1f;

            if(_leftoverDeltaMax > 0.9f / (1000f / _tabletHz))
            {
                _leftoverDeltaMax = 0.9f / _tabletHz;
                _leftoverDeltaMin = -_leftoverDeltaMax;
            }


            //***

            _runCalc = false;

            _smoothTransitionVector = Vector2.Zero;

            FilterWeightInitialisation();
        }

        private bool FilterCheckDuplicate(Vector2 Position) //For pens that output an extra packet with duplicate position on button press or pressure, mostly.
        {
            Vector2 PredictedSpeedVector = Vector2.Zero;

            for (int d = 1; d < _derivatives; d++)
                PredictedSpeedVector += _bestD[_t[0], d];

            float PredictedSpeed = PredictedSpeedVector.Length();

            if (PredictedSpeed > _ignoreDuplicateVelocity
                && _rawD[0, 0] == Position)
                return true;
            else
                return false;
        }

        private void FilterStateReset(Vector2 Position) //After receiving new position after long time
        {
            //Buffer pointer reset

            for (int t = 0; t < _tSize; t++)
                _t[t] = t;

            //Position reset

            _rawD[_t[0], 0] = Position;
            for (int t = 0; t < _tSize; t++)
            {
                _rawD[_t[t], 0] = Position;

                _rawDDCat[_t[t], 0] = 0;
                _rawDMCat[_t[t], 0] = 0;

                _bestD[_t[t], 0] = Position;
                for (int d = 1; d < _derivatives; d++)
                {
                    _rawD[_t[t], d] = Vector2.Zero;

                    _rawDDCat[_t[t], d] = 0;
                    _rawDMCat[_t[t], d] = 0;
    
                    _bestD[_t[t], d] = Vector2.Zero;
                }
            }

            //Timing reset

            _reportWatch.Restart();

            _recentDelta = _reportDelta;

            _interpTime = 0f;

            _leftoverDelta = 0f;

            _interpSpeedMulti = 1f;

            _smoothTransitionVector = Vector2.Zero;

            _runCalc = false;
        }

        private void FilterStateUpdate(Vector2 Position)
        {

            /* Timing update
             */

            _recentDelta = (float)_reportWatch.Restart().TotalMilliseconds;

            Vector2 OldPosition = FilterInterpolate(_recentDelta); // Smooth transition calc thing

            _reportDelta = _reportDelta * 0.9999f + 0.0001f * _recentDelta;

            _leftoverDelta = _recentDelta - _reportDelta;

            if (_leftoverDelta < _leftoverDeltaMin && _leftoverDelta > -_reportDelta)
                _leftoverDeltaMin = _leftoverDeltaMin * 0.98f + 0.02f * _leftoverDelta;
            else if (_leftoverDelta < 0f)
                _leftoverDeltaMin = 0.999f * _leftoverDeltaMin + 0.001f * _leftoverDelta;

            if (_leftoverDelta > _leftoverDeltaMax && _leftoverDelta < _reportDelta)
                _leftoverDeltaMax = _leftoverDeltaMax * 0.98f + 0.02f * _leftoverDelta;
            else if (_leftoverDelta > 0f)
                _leftoverDeltaMax = 0.999f * _leftoverDeltaMax + 0.001f * _leftoverDelta;


            var MaxReportLength = _reportDelta + _leftoverDeltaMax;

            _interpTime = _interpLeftOver + _recentDelta * _interpSpeedMulti;

            _interpLeftOver = _interpTime - _reportDelta;

            float InterpCatchupRatio = 0.5f;

            if (_interpLeftOver > _leftoverDeltaMax)
            {
                _interpSpeedMulti = (MaxReportLength + InterpCatchupRatio * (_leftoverDeltaMax - _interpLeftOver)) / (MaxReportLength);
            }
            else if (_interpLeftOver < _leftoverDeltaMin)
            {
                _interpSpeedMulti = (MaxReportLength + InterpCatchupRatio * (_leftoverDeltaMin - _interpLeftOver)) / (MaxReportLength);
            }
            else
                _interpSpeedMulti = 1f;

            /* Time/buffer pointer update
             */

            for (int t = 0; t < _tSize - 1; t++)
                _t[t + 1] = _t[t];

            _t[0]++;
            if (_t[0] >= _tSize)
                _t[0] = 0;

            /* Position and state estimate update
             */

            _rawD[_t[0], 0] = Position;
            for (int d = 1; d < _derivatives; d++)
                _rawD[_t[0], d] = _rawD[_t[0], d - 1] - _bestD[_t[1], d - 1];


            for (int d = 0; d < _derivatives; d++)
            {
                float DChange =   (_bestD[_t[1], d] - _rawD[_t[0], d]).Length();

                if (System.MathF.Sqrt( 2.19f * DChange ) >= (float)System.Int32.MaxValue - 1f)
                    _rawDDCat[_t[0], d] = _categories - 1;
                else
                _rawDDCat[_t[0], d] = System.Convert.ToInt32( //condense length information with more accuracy for lower values, where most of the tablet noise happens, could probablyh use log too
                                            System.MathF.Sqrt(
                                                2.19f * DChange
                                            )
                                        );
                if (_rawDDCat[_t[0], d] > _categories - 1)
                    _rawDDCat[_t[0], d] = _categories - 1;

                float BestWeight = _weight[_dCategoryStateWeight[_rawDDCat[_t[0], d], d]];

                //if (BestWeight != 0)
                    _bestD[_t[0], d] =      + BestWeight
                                            * _rawD[_t[0], d]
                                            + (1f - BestWeight)
                                            * _bestD[_t[1], d];

                float DMagnitude = _bestD[_t[0], d].Length();
                //else
                //    _bestD[_t[0], d] = Vector2.Zero;
                if (System.MathF.Sqrt( 2.19f * DMagnitude ) >= (float)System.Int32.MaxValue - 1f)
                    _rawDDCat[_t[0], d] = _categories - 1;
                else
                    _rawDMCat[_t[0], d] = System.Convert.ToInt32( //condense length information with more accuracy for lower values, where most of the tablet noise happens, could probablyh use log too
                            System.MathF.Sqrt(
                                2.19f * DMagnitude
                            )
                        );

                if (_rawDMCat[_t[0], d] > _categories - 1)
                    _rawDMCat[_t[0], d] = _categories - 1;



                //if (BestWeight == 0f)
                //{
                //    for (int i = d; i < _derivatives; i++)
                //        _bestD[_t[0], i] = Vector2.Zero;
                //
                //    d = _derivatives;
                //}
            }

            _smoothTransitionVector = Vector2.Zero; //this is looked up by FilterInterpolate so you gotta zero it.
            _smoothTransitionVector = OldPosition - FilterInterpolate(0f);
        }

        public override PipelinePosition Position => PipelinePosition.PreTransform;
        protected override void ConsumeState()
        {
            if (State is ITabletReport tabletReport)
            {
                if (_initialise)
                {
                    FilterInitialise();
                    _initialise = false;
                }

                if (_ignoreDuplicate) //Ignore duplicates when previous cursor movement was faster than something, mb need to make it cleaner still
                    if (FilterCheckDuplicate(tabletReport.Position))
                    {
                        tabletReport.Position = _outputPosition;
                        OnEmit();
                        return;
                    }

                _rawPosition = tabletReport.Position;
                _pressure = tabletReport.Pressure;

                if (_stateReset)
                {
                    _stateReset = false;
                    FilterStateReset(_rawPosition);
                }
                else
                { 
                    FilterStateUpdate(_rawPosition);

                    _runCalc = true;
                    //FilterStateUpdatePrediction();
                }

                Console.WriteLine(check);
                check = 0;

                //if (_interpolation == false)
                //{
                //    _outputPosition = FilterInterpolate(0f);
                //    tabletReport.Position = _outputPosition;
                //    tabletReport.Pressure = _pressure;
                //    OnEmit();
                //}
            }
            else
            {
                OnEmit();
            }
        }

        protected override void UpdateState()
        {
            float ReportWatchElapsed = (float)_reportWatch.Elapsed.TotalMilliseconds;

            if (State is ITabletReport tabletReport && ReportWatchElapsed < 1.5f + _reportDelta + 2 * _leftoverDeltaMax - _leftoverDeltaMin)
            {
                //if (_interpolation == true)
                //{
                    _outputPosition = FilterInterpolate(ReportWatchElapsed);
                    tabletReport.Position = _outputPosition;
                    check += (Math.Sqrt(Math.Pow(_rawPosition.X - _outputPosition.X, 2) + Math.Pow(_rawPosition.Y - _outputPosition.Y, 2)));
                    tabletReport.Pressure = _pressure;
                    OnEmit();
                //}

                if (_runCalc) 
                {
                    FilterStateUpdatePrediction();
                    _runCalc = false;

                    _statCalcTime = ((float)_reportWatch.Elapsed.TotalMilliseconds - ReportWatchElapsed) * 0.01f + 0.99f * _statCalcTime;
                }

                if(_duplicate > 0)
                for(int d = 1; d <= _duplicate; d++)
                {
                     _outputPosition = FilterInterpolate((float)d / (float)(_duplicate + 1) * (1000f / Frequency) + ReportWatchElapsed);

                    while ((float)_reportWatch.Elapsed.TotalMilliseconds < (float)d / (float)(_duplicate + 1) * (1000f/Frequency)  + ReportWatchElapsed)
                    {
                        d++;
                        d--;
                    }
                    //_outputPosition = FilterInterpolate((float)_reportWatch.Elapsed.TotalMilliseconds);
                    tabletReport.Position = _outputPosition;
                    
                    tabletReport.Pressure = _pressure;
                    OnEmit();
                }
        }
            else if (_stateReset != true)
            {
                //FilterNormaliseError();
                _reportWatch.Stop();
                _stateReset = true;

                System.Console.WriteLine("Calculation time: " + _statCalcTime / (1000f / Frequency) +". If higher than or close to 1, reduce weights or derivatives.");
            }
        }

        public double check;

    }
}