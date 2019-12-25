using AYLib.GenericDataLogger;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericDataLoggerBenchmarks
{
    public class Program
    {
        [InProcessAttribute]
        public class WriterBenchmarks
        {
            WriteDataBuffer sut;
            MemoryStream ms;
            //DirectSerializeWriter writerSut;

            public WriterBenchmarks()
            {
                sut = new WriteDataBuffer();
                ms = new MemoryStream();
            }

            [Benchmark]
            public void Test()
            {
                for (int i = 0; i < 1000000; i++)
                {
                    sut.WriteDataBlock(Guid.Empty.ToByteArray(), 0, 0, 0, false);
                    sut.WriteTo(ms);
                    //ms.Position = 0;
                }
            }
        }
        
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<WriterBenchmarks>();

            Console.ReadKey();
        }
    }
}
