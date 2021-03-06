﻿// 
// Copyright (c) 2004-2011 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog.Common;
using NLog.Internal;
using Xunit;
using Xunit.Extensions;

namespace NLog.UnitTests.Internal
{
    public class ExceptionHelperTests : NLogTestBase
    {
        [Theory]
        [InlineData(typeof(StackOverflowException), true)]
        [InlineData(typeof(NLogConfigurationException), false)]
        [InlineData(typeof(Exception), false)]
        [InlineData(typeof(ArgumentException), false)]
        [InlineData(typeof(NullReferenceException), false)]
        [InlineData(typeof(ThreadAbortException), true)]
        [InlineData(typeof(OutOfMemoryException), true)]
        public void TestMustBeRethrowImmediately(Type t, bool result)
        {
            var ex = CreateException(t);
            Assert.Equal(result, ex.MustBeRethrownImmediately());

        }

        [Theory]
        [InlineData(typeof(StackOverflowException), true, false)]
        [InlineData(typeof(StackOverflowException), true, true)]
        [InlineData(typeof(NLogConfigurationException), true, false)]
        [InlineData(typeof(NLogConfigurationException), true, true)]
        [InlineData(typeof(Exception), false, false)]
        [InlineData(typeof(Exception), true, true)]
        [InlineData(typeof(ArgumentException), false, false)]
        [InlineData(typeof(ArgumentException), true, true)]
        [InlineData(typeof(NullReferenceException), false, false)]
        [InlineData(typeof(NullReferenceException), true, true)]
        [InlineData(typeof(ThreadAbortException), true, false)]
        [InlineData(typeof(ThreadAbortException), true, true)]
        [InlineData(typeof(OutOfMemoryException), true, false)]
        [InlineData(typeof(OutOfMemoryException), true, true)]
        public void MustBeRethrown(Type exceptionType, bool result, bool throwExceptions)
        {
            var throws = LogManager.ThrowExceptions;
            try
            {
                LogManager.ThrowExceptions = throwExceptions;

                var ex = CreateException(exceptionType);
                Assert.Equal(result, ex.MustBeRethrown());
            }
            finally
            {
                //restore
                LogManager.ThrowExceptions = throws;
            }

        }

        [Theory]
        [InlineData("Error has been raised.", typeof(ArgumentException), false, "Error")]
        [InlineData("Error has been raised.", typeof(ArgumentException), true, "Warn")]
        [InlineData("Error has been raised.", typeof(NLogConfigurationException), false, "Warn")]
        [InlineData("Error has been raised.", typeof(NLogConfigurationException), true, "Warn")]
        [InlineData("", typeof(ArgumentException), true, "Warn")]
        [InlineData("", typeof(NLogConfigurationException), true, "Warn")]
        
        public void MustBeRethrown_ShouldLog_exception_and_only_once(string text, Type exceptionType, bool logFirst, string levelText)
        {
            using (new InternalLoggerScope())
            {

                var level = LogLevel.FromString(levelText);
                InternalLogger.LogLevel = LogLevel.Trace;
                InternalLogger.LogToConsole = true;
                InternalLogger.IncludeTimestamp = false;

                var ex1 = CreateException(exceptionType);
              

                //exception should be once 
                const string prefix = " Exception: ";
                string expected =
                    levelText + " " + text + prefix + ex1 + Environment.NewLine;

                StringWriter consoleOutWriter = new StringWriter()
                {
                    NewLine = Environment.NewLine
                };

                // Redirect the console output to a StringWriter.
                Console.SetOut(consoleOutWriter);

                // Named (based on LogLevel) public methods.

                if (logFirst)
                    InternalLogger.Log(ex1, level, text);

                ex1.MustBeRethrown();

                consoleOutWriter.Flush();
                var actual = consoleOutWriter.ToString();
                Assert.Equal(expected, actual);
            }


        }

        private static Exception CreateException(Type exceptionType)
        {
            return exceptionType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null).Invoke(null) as Exception;
        }
    }
}
#endif