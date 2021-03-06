﻿using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.InterIntegratedCircuit;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace GoPiGo
{
    public interface IGoPiGo : IDisposable
    {
        string GetFirmwareVersion();
        byte DigitalRead(Pin pin);
        void DigitalWrite(Pin pin, byte value);
        int AnalogRead(Pin pin);
        void AnalogWrite(Pin pin, byte value);
        void PinMode(Pin pin, PinMode mode);
        decimal BatteryVoltage();
        IMotorController MotorController();
        //Currently not functioning
        //IEncoderController EncoderController();
        IGoPiGo RunCommand(Commands command, byte firstParam = Constants.Unused, byte secondParam = Constants.Unused, byte thirdParam = Constants.Unused);
    }

    public class GoPiGo : IGoPiGo
    {
        const int GoPiGoAddress = 0x08;
        private readonly IMotorController _motorController;
        private readonly IEncoderController _encoderController;
        internal I2cDriver Driver { get; }
        internal I2cDeviceConnection I2CController { get; }
        internal GoPiGo()
        {
            this.Driver = new I2cDriver(ProcessorPin.Pin2, ProcessorPin.Pin3);
            this.I2CController = Driver.Connect(GoPiGoAddress);
            _motorController = new MotorController(this);
            _encoderController = new EncoderController(this);
        }

        internal bool WriteToI2C(byte[] block){
            try
            {
                I2CController.Write(block);
                Thread.Sleep(5);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
        public IMotorController MotorController()
        {
            return _motorController;
        }

        public IEncoderController EncoderController()
        {
            return _encoderController;
        }


        public string GetFirmwareVersion()
        {
            var buffer = new[] { (byte)Commands.Version, Constants.Unused, Constants.Unused, Constants.Unused };
            WriteToI2C(buffer);
            var firmware = I2CController.ReadByte();
            return $"{firmware}";
        }

        public byte DigitalRead(Pin pin)
        {
            var buffer = new[] { (byte)Commands.DigitalRead, (byte)pin, Constants.Unused, Constants.Unused };
            WriteToI2C(buffer);
            Thread.Sleep(100);
            var data = I2CController.ReadByte();
            return data;
        }

        public void DigitalWrite(Pin pin, byte value)
        {
            var buffer = new[] { (byte)Commands.DigitalWrite, (byte)pin, value, Constants.Unused };
            WriteToI2C(buffer);

        }

        public int AnalogRead(Pin pin)
        {
            var buffer = new[]
            {(byte) Commands.AnalogRead, (byte) pin, Constants.Unused, Constants.Unused};
            WriteToI2C(buffer);
            Thread.Sleep(7);
            try
            {
                var b1 = I2CController.ReadByte();
                var b2 = I2CController.ReadByte();
                return b1 * 256 + b2;
            }
            catch (Exception)
            {
                return -1;
            }

        }

        public void AnalogWrite(Pin pin, byte value)
        {
            var buffer = new[] { (byte)Commands.AnalogWrite, (byte)pin, value, Constants.Unused };
            WriteToI2C(buffer);
        }

        public void PinMode(Pin pin, PinMode mode)
        {
            var buffer = new[] { (byte)Commands.PinMode, (byte)pin, (byte)mode, Constants.Unused };
            WriteToI2C(buffer);
        }


        public decimal BatteryVoltage()
        {
            var buffer = new[] { (byte)Commands.BatteryVoltage, Constants.Unused, Constants.Unused, Constants.Unused };
            WriteToI2C(buffer);
            Thread.Sleep(1); //Wait a few ms to process
            try
            {
                var b1 = I2CController.ReadByte();
                var b2 = I2CController.ReadByte();
                decimal v = b1 * 256 + b2;
                v = (5 * v / 1024) / (decimal)0.4;
                return Math.Round(v, 2);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public IGoPiGo RunCommand(Commands command, byte firstParam = Constants.Unused, byte secondParam = Constants.Unused, byte thirdParam = Constants.Unused)
        {
            var buffer = new[] { (byte)command, firstParam, secondParam, thirdParam };
            WriteToI2C(buffer);
            Thread.Sleep(5);
            return this;
        }

        public void Dispose()
        {
            if (this.Driver != null)
            {
                this.MotorController().Stop();
                this.Driver.Dispose();
            }
        }
    }
}
