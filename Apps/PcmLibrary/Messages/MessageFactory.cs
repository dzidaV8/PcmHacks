﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PcmHacking
{
    public enum BlockCopyType
    {
        // Copy to RAM or Flash
        Copy = 0x00,

        // Execute after copying to RAM
        Execute = 0x80,

        // Test copy to flash, but do not unlock or actually write.
        TestWrite = 0x44,    
    };

    /// <summary>
    /// This class is responsible for generating the messages that the app sends to the PCM.
    /// </summary>
    /// <remarks>
    /// The messages generated by this class are byte-for-byte exactly what the PCM 
    /// receives, with the exception of the CRC byte at the end. CRC bytes must be 
    /// added by the currently-selected Device class if the actual device doesn't add 
    /// the CRC byte automatically.
    ///
    /// Some devices will require these messages to be translated according to the specific
    /// device's protocol - that too is the job of the currently-selected Device class.
    /// </remarks>
    public partial class Protocol
    {
        /// <summary>
        /// Create a request to read the given block of PCM memory.
        /// </summary>
        public Message CreateReadRequest(byte Block)
        { 
            byte[] Bytes = new byte[] { Priority.Physical0, DeviceId.Pcm, DeviceId.Tool, Mode.ReadBlock, Block };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a request to read the PCM's operating system ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateOperatingSystemIdReadRequest()
        {
            return CreateReadRequest(BlockId.OperatingSystemID);
        }

        /// <summary>
        /// Create a request to read the PCM's Calibration ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateCalibrationIdReadRequest()
        {
            return CreateReadRequest(BlockId.CalibrationID);
        }

        /// <summary>
        /// Create a request to read the PCM's Hardware ID.
        /// </summary>
        /// <returns></returns>
        public Message CreateHardwareIdReadRequest()
        {
            return CreateReadRequest(BlockId.HardwareID);
        }

        /// <summary>
        /// Create a request to read the first segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest1()
        {
            return CreateReadRequest(BlockId.Vin1);
        }

        /// <summary>
        /// Create a request to read the second segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest2()
        {
            return CreateReadRequest(BlockId.Vin2);
        }

        /// <summary>
        /// Create a request to read the thid segment of the PCM's VIN.
        /// </summary>
        public Message CreateVinRequest3()
        {
            return CreateReadRequest(BlockId.Vin3);
        }

        /// <summary>
        /// Create a request to read the first segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest1()
        {
            return CreateReadRequest(BlockId.Serial1);
        }

        /// <summary>
        /// Create a request to read the second segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest2()
        {
            return CreateReadRequest(BlockId.Serial2);
        }

        /// <summary>
        /// Create a request to read the thid segment of the PCM's Serial Number.
        /// </summary>
        public Message CreateSerialRequest3()
        {
            return CreateReadRequest(BlockId.Serial3);
        }

        /// <summary>
        /// Create a request to read the Broad Cast Code (BCC).
        /// </summary>
        public Message CreateBCCRequest()
        {
            return CreateReadRequest(BlockId.BCC);
        }

        /// <summary>
        /// Create a request to read the Broad Cast Code (MEC).
        /// </summary>
        public Message CreateMECRequest()
        {
            return CreateReadRequest(BlockId.MEC);
        }

        /// <summary>
        /// Create a request to retrieve a 'seed' value from the PCM
        /// </summary>
        public Message CreateSeedRequest()
        {
            byte[] Bytes = new byte[] { Priority.Physical0, DeviceId.Pcm, DeviceId.Tool, Mode.Seed, SubMode.GetSeed };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a request to send a 'key' value to the PCM
        /// </summary>
        public Message CreateUnlockRequest(UInt16 Key)
        {
            byte KeyHigh = (byte)((Key & 0xFF00) >> 8);
            byte KeyLow = (byte)(Key & 0xFF);
            byte[] Bytes = new byte[] { Priority.Physical0, DeviceId.Pcm, DeviceId.Tool, Mode.Seed, SubMode.SendKey, KeyHigh, KeyLow };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a block message from the supplied arguments.
        /// </summary>
        public Message CreateBlockMessage (byte[] Payload, int Offset, int Length, int Address, BlockCopyType copyType)
        {
            byte[] Buffer = new byte[10 + Length + 2];
            byte[] Header = new byte[10];
        
            byte Size1 = unchecked((byte)(Length >> 8));
            byte Size2 = unchecked((byte)(Length & 0xFF));
            byte Addr1 = unchecked((byte)(Address >> 16));
            byte Addr2 = unchecked((byte)(Address >> 8));
            byte Addr3 = unchecked((byte)(Address & 0xFF));

            Header[0] = Priority.Block;
            Header[1] = DeviceId.Pcm;
            Header[2] = DeviceId.Tool;
            Header[3] = Mode.PCMUpload;
            Header[4] = (byte)copyType;
            Header[5] = Size1;
            Header[6] = Size2;
            Header[7] = Addr1;
            Header[8] = Addr2;
            Header[9] = Addr3;

            System.Buffer.BlockCopy(Header, 0, Buffer, 0, Header.Length);
            System.Buffer.BlockCopy(Payload, Offset, Buffer, Header.Length, Length);

            return new Message(AddBlockChecksum(Buffer));
        }

        /// <summary>
        /// Tell the bus that a test device is present.
        /// </summary>
        public Message CreateDevicePresentNotification()
        {
            return new Message(new byte[] { 0x8C, 0xFE, DeviceId.Tool, 0x3F });
        }
        
        /// <summary>
        /// Create a request to read an arbitrary address range.
        /// </summary>
        /// <remarks>
        /// This command is only understood by the reflash kernel.
        /// </remarks>
        /// <param name="startAddress">Address of the first byte to read.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns></returns>
        public Message CreateReadRequest(int startAddress, int length)
        {
            byte[] request = { 0x6D, DeviceId.Pcm, DeviceId.Tool, 0x35, 0x01, (byte)(length >> 8), (byte)(length & 0xFF), (byte)(startAddress >> 16), (byte)((startAddress >> 8) & 0xFF), (byte)(startAddress & 0xFF) };
            byte[] request2 = { 0x6D, DeviceId.Pcm, DeviceId.Tool, 0x37, 0x01, (byte)(length >> 8), (byte)(length & 0xFF), (byte)(startAddress >> 24), (byte)(startAddress >> 16), (byte)((startAddress >> 8) & 0xFF), (byte)(startAddress & 0xFF) };

            if (startAddress > 0xFFFFFF)
            {
                return new Message(request2);
            }
            else
            {
                return new Message(request);
            }
        }

        /// <summary>
        /// Write a 16 bit sum to the end of a block, returns a Message, as a byte array
        /// </summary>
        /// <remarks>
        /// Overwrites the last 2 bytes at the end of the array with the sum
        /// </remarks>
        public byte[] AddBlockChecksum(byte[] Block)
        {
            UInt16 Sum = 0;

            for (int i = 4; i < Block.Length-2; i++) // skip prio, dest, src, mode
            {
                Sum += Block[i];
            }

            Block[Block.Length - 2] = unchecked((byte)(Sum >> 8));
            Block[Block.Length - 1] = unchecked((byte)(Sum & 0xFF));

            return Block;
        }
        
        /// <summary>
        /// Create a request for a module to test VPW speed switch to 4x is OK
        /// </summary>
        public Message CreateHighSpeedPermissionRequest(byte deviceId)
        {
            return new Message(new byte[] { Priority.Physical0, deviceId, DeviceId.Tool, Mode.HighSpeedPrepare});
        }
        
        /// <summary>
        /// Create a request for a specific module to switch to VPW 4x
        /// </summary>
        public Message CreateBeginHighSpeed(byte deviceId)
        {
            return new Message(new byte[] { Priority.Physical0, deviceId, DeviceId.Tool, Mode.HighSpeed });
        }
        
        /// <summary>
        /// Create a broadcast message announcing there is a test device connected to the vehicle
        /// </summary>
        public Message CreateTestDevicePresent()
        {
            byte[] bytes = new byte[] { Priority.Physical0High, DeviceId.Broadcast, DeviceId.Tool, Mode.TestDevicePresent };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a message to tell the RAM-resident kernel to exit.
        /// </summary>
        public Message CreateExitKernel()
        {
            byte[] bytes = new byte[] { Priority.Physical0, DeviceId.Pcm, DeviceId.Tool, 0x20 };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a broadcast message telling the PCM to clear DTCs
        /// </summary>
        public Message CreateClearDTCs()
        {
            byte[] bytes = new byte[] { Priority.Functional0, 0x6A, DeviceId.Tool, Mode.ClearDTCs };
            return new Message(bytes);
        }

        /// <summary>
        /// A successfull response seen after the Clear DTCs message
        /// </summary>
        public Message CreateClearDTCsOK()
        {
            byte[] bytes = new byte[] { 0x48, 0x6B, DeviceId.Pcm, Mode.ClearDTCs + Mode.Response };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a broadcast message telling all devices to disable normal message transmission (disable chatter)
        /// </summary>
        public Message CreateDisableNormalMessageTransmission()
        {
            byte[] Bytes = new byte[] { Priority.Physical0, DeviceId.Broadcast, DeviceId.Tool, Mode.SilenceBus, SubMode.Null };
            return new Message(Bytes);
        }

        /// <summary>
        /// Create a broadcast message telling all devices to disable normal message transmission (disable chatter)
        /// </summary>
        public Message CreateDisableNormalMessageTransmissionOK()
        {
            byte[] bytes = new byte[] { Priority.Physical0, DeviceId.Tool, DeviceId.Pcm, Mode.SilenceBus + Mode.Response , SubMode.Null };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a broadcast message telling all devices to clear their DTCs
        /// </summary>
        public Message ClearDTCs()
        {
            byte[] bytes = new byte[] { Priority.Functional0, 0x6A, DeviceId.Tool, Mode.ClearDTCs };
            return new Message(bytes);
        }
        
        /// <summary>
        /// PCM Response to Clear DTCs
        /// </summary>
        public Message ClearDTCsOK()
        {
            byte[] bytes = new byte[] { Priority.Functional0Low, 0x6B, DeviceId.Pcm, Mode.ClearDTCs + Mode.Response };
            return new Message(bytes);
        }

        /// <summary>
        /// Create a request to uploade size bytes to the given address
        /// </summary>
        /// <remarks>
        /// Note that mode 0x34 is only a request. The actual payload is sent as a mode 0x36.
        /// </remarks>
        public Message CreateUploadRequest(int Address, int Size)
        {
            byte[] requestBytes = { Priority.Physical0, DeviceId.Pcm, DeviceId.Tool, Mode.PCMUploadRequest, SubMode.Null, 0x00, 0x00, 0x00, 0x00, 0x00 };
            requestBytes[5] = unchecked((byte)(Size >> 8));
            requestBytes[6] = unchecked((byte)(Size & 0xFF));
            requestBytes[7] = unchecked((byte)(Address >> 16));
            requestBytes[8] = unchecked((byte)(Address >> 8));
            requestBytes[9] = unchecked((byte)(Address & 0xFF));
            
            return new Message(requestBytes);
        }


        ///////////////////////////////////////////////////////////////////////
        // Mode 3D was apparently not used for anything, so it's being taken
        // for flash-chip and CRC queries.
        ///////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Create a request for the kernel version.
        /// </summary>
        public Message CreateKernelVersionQuery()
        {
            return new Message(new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0x00 });
        }

        /// <summary>
        /// Create a request to identify the flash chip. 
        /// </summary>
        public Message CreateFlashMemoryTypeQuery()
        {
            return new Message(new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0x01 });
        }

        /// <summary>
        /// Create a request to get the CRC of a byte range.
        /// </summary>
        public Message CreateCrcQuery(UInt32 address, UInt32 size)
        {
            byte[] requestBytes = new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            requestBytes[5] = unchecked((byte)(size >> 16));
            requestBytes[6] = unchecked((byte)(size >> 8));
            requestBytes[7] = unchecked((byte)size);
            requestBytes[8] = unchecked((byte)(address >> 16));
            requestBytes[9] = unchecked((byte)(address >> 8));
            requestBytes[10] = unchecked((byte)address);
            return new Message(requestBytes);
        }

        /// <summary>
        /// Create a request for implementation details... for development use only.
        /// </summary>
        public Message CreateDebugQuery()
        {
            return new Message(new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0xFF });
        }

        ///////////////////////////////////////////////////////////////////////
        // Mode 3E was apparently not used for anything, so it's being taken
        // for flash-chip lock, unlock, and erase.
        ///////////////////////////////////////////////////////////////////////


        public Message CreateFlashUnlockRequest()
        {
            return new Message(new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0x03 });
        }

        public Message CreateFlashLockRequest()
        {
            return new Message(new byte[] { 0x6C, 0x10, 0xF0, 0x3D, 0x04 });
        }

        public Message CreateFlashEraseBlockRequest(UInt32 baseAddress)
        {
            return new Message(new byte[]
            {
                0x6C,
                0x10,
                0xF0,
                0x3D,
                0x05,
                (byte)(baseAddress >> 16),
                (byte)(baseAddress >> 8),
                (byte)baseAddress
            });
        }
    }
}
