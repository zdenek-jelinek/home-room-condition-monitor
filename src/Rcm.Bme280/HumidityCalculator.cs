﻿using System;

namespace Rcm.Bme280
{
    public class HumidityCalculator
    {
        private readonly HumidityCompensationParameters _compensationParameters;

        public HumidityCalculator(HumidityCompensationParameters compensationParameters)
        {
            _compensationParameters = compensationParameters;
        }

        public decimal CalculateHumidity(int rawHumidity, int fineTemperature)
        {
            var init = fineTemperature - 76800;

            var x1 = (((rawHumidity << 14)
                        - (_compensationParameters.Humidity4 << 20)
                        - _compensationParameters.Humidity5 * init
                        + 16384)
                    >> 15)
                * (((((((init * _compensationParameters.Humidity6) >> 10)
                                    * (((init * _compensationParameters.Humidity3) >> 11) + 32768))
                                >> 10)
                            + 2097152)
                        * _compensationParameters.Humidity2
                        + 8192)
                    >> 14);

            var x2 = x1
                - (((((x1 >> 15) * (x1 >> 15)) >> 7) * _compensationParameters.Humidity1) >> 4);

            var humidity = (uint)(x2 >> 12) / 1024m;

            return Math.Max(0m, Math.Min(100m, humidity));
        }
    }
}