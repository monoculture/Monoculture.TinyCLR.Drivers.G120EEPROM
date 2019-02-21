using System;
using System.Runtime.InteropServices;

namespace Monoculture.TinyCLR.Drivers.G120EEPROM
{
    public class G120EEPROMDriver
    {
        public const uint PageSize = 64;
        private const int CMD_8BITS_READ = 0;
        private const int CMD_8BITS_WRITE = 3;
        private const int CMD_RDPREFETCH = 1 << 3;
        private const int RDWR_CLR_ST = 1 << 26;
        private const int INT_ENDOFPROG = 1 << 28;
        private const int CMD_ERASE_PRG_PAGE = 6;

        private readonly IntPtr EECMD = new IntPtr(0x00200080);
        private readonly IntPtr EEADDR = new IntPtr(0x00200084);
        private readonly IntPtr EEWDATA = new IntPtr(0x00200088);
        private readonly IntPtr EERDATA = new IntPtr(0x0020008C);
        private readonly IntPtr EEWSTATE = new IntPtr(0x00200090);
        private readonly IntPtr EECLKDIV = new IntPtr(0x00200094);
        private readonly IntPtr PWRDWN = new IntPtr(0x00200098);
        private readonly IntPtr STAT = new IntPtr(0x00200FE0);
        private readonly IntPtr STATCLR = new IntPtr(0x00200FE8);

        public G120EEPROMDriver(uint speed = 120000000)
        {
            Marshal.WriteInt32(PWRDWN, 0);

            Marshal.WriteInt32(EECLKDIV, (int)speed / 375000 - 1);

            var phase1 = (byte)(speed / 1000000 * 15 / 1000 + 1);

            var phase2 = (byte)(speed / 1000000 * 55 / 1000 + 1);

            var phase3 = (byte)(speed / 1000000 * 35 / 1000 + 1);

            var states = phase1 | phase2 << 8 | phase3 << 16;

            Marshal.WriteInt32(EEWSTATE, states);
        }

        /// <summary>
        /// Reads data from the internal EEPROM
        /// </summary>
        /// <param name="page">0-62</param>
        /// <param name="address">0-63</param>
        /// <param name="size">Size of data</param>
        /// <returns>Data read from EEPROM</returns>
        public byte[] Read(uint page, uint address, uint size)
        {
            if(page > 62)
                throw new ArgumentOutOfRangeException(nameof(page));

            if (address > 63)
                throw new ArgumentOutOfRangeException(nameof(address));

            if(page * address + size > 4096)
                throw new ArgumentOutOfRangeException(nameof(size));

            var readBuffer = new byte[size];

            // Clear read/write status
            Marshal.WriteInt32(STATCLR, RDWR_CLR_ST);

            // Write address register
            Marshal.WriteInt32(EEADDR, (int)((page & 0x3F) << 6 | address & 0x3F));

            // Set command to read 8 bits + prefetch next address
            Marshal.WriteInt32(EECMD, CMD_8BITS_READ | CMD_RDPREFETCH);

            // Loop through buffer.
            for (var i = 0; i < readBuffer.Length; i++)
            {
                readBuffer[i] = Marshal.ReadByte(EERDATA);

                WaitForInterruptStatus(RDWR_CLR_ST);

                address++;

                if (address < PageSize) continue;

                page++;

                address = 0;

                // Write address register
                Marshal.WriteInt32(EEADDR, (int)((page & 0x3F) << 6 | address & 0x3F));

                // Set command to read 8 bits + prefetch next address
                Marshal.WriteInt32(EECMD, CMD_8BITS_READ | CMD_RDPREFETCH);
            }

            return readBuffer;
        }

        /// <summary>
        /// Writes data to the internal EEPROM
        /// </summary>
        /// <param name="page">0-62</param>
        /// <param name="address">0-63</param>
        /// <param name="data">Data to write</param>
        public void Write(uint page, uint address, byte[] data)
        {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            if (page > 62)
                throw new ArgumentOutOfRangeException(nameof(page));

            if (address > 63)
                throw new ArgumentOutOfRangeException(nameof(address));

            // set address
            Marshal.WriteInt32(EEADDR, (int)((page & 0x3F) << 6 | address & 0x3F));

            // set write 8-bit
            Marshal.WriteInt32(EECMD, CMD_8BITS_WRITE);

            for (var i = 0; i < data.Length; i++)
            {
                // write 8 bits to page register
                Marshal.WriteInt32(EEWDATA, data[i]);

                WaitForInterruptStatus(RDWR_CLR_ST);

                address++;

                if (address < PageSize) continue;

                // clear  page erase status
                Marshal.WriteInt32(STATCLR, CMD_ERASE_PRG_PAGE);

                // set address to base of current page
                Marshal.WriteInt32(EEADDR, (int)((page & 0x3F) << 6 | 0 & 0x3F));

                // Program current page
                Marshal.WriteInt32(EECMD, CMD_ERASE_PRG_PAGE);

                // Wait for operation to complete
                WaitForInterruptStatus(INT_ENDOFPROG);

                page++;

                address = 0;

                Marshal.WriteInt32(EEADDR, (int)((page & 0x3F) << 6 | address & 0x3F));

                // set write 8-bit
                Marshal.WriteInt32(EECMD, CMD_8BITS_WRITE);
            }
        }

        private void WaitForInterruptStatus(int mask)
        {
            while (true)
            {
                var status = Marshal.ReadInt32(STAT);

                if ((status & mask) == mask)
                {
                    break;
                }
            }
        }
    }
}