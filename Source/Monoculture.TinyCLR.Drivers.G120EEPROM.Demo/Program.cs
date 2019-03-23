/*
 * Copyright 2019 David Smith
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Monoculture.TinyCLR.Drivers.G120EEPROM.Demo
{
    class Program
    {
        public class Settings
        {
            public int Property1 { get; set; }
            public int Property2 { get; set; }
        }

        static void Main()
        {
            var eeprom = new G120EEPROMDriver();

            var settings = new Settings();

            LoadSettings(eeprom, settings);

            settings.Property1 = 100;
            settings.Property2 = 200;

            SaveSettings(eeprom, settings);
        }

        private static void LoadSettings(G120EEPROMDriver eeprom, Settings settings)
        {
            var buffer = eeprom.Read(0, 0, G120EEPROMDriver.PageSize);

            settings.Property1 = BitConverter.ToInt32(buffer, 0);
            settings.Property2 = BitConverter.ToInt32(buffer, 4);
        }

        private static void SaveSettings(G120EEPROMDriver eeprom, Settings settings)
        {
            var buffer = new byte[G120EEPROMDriver.PageSize];

            BitConverter.GetBytes(settings.Property1).CopyTo(buffer, 0);
            BitConverter.GetBytes(settings.Property2).CopyTo(buffer, 4);

            eeprom.Write(0, 0, buffer);
        }
    }
}
