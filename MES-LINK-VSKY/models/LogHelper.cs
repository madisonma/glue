using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
namespace MES_LINK_VSKY.models
{
        public class LogHelper
    {
        private static readonly Logger log = LogManager.GetLogger("");
        public static void Error(object msg, Exception exp = null)
        {
            if (exp == null)
                log.Error("#" + msg);
            else
                log.Error("#" + msg + "  " + exp.ToString());
        }

        public static void Debug(object msg, Exception exp = null)
        {
            if (exp == null)
                log.Debug("#" + msg);
            else
                log.Debug("#" + msg + "  " + exp.ToString());
        }

        public static void Info(object msg, Exception exp = null)
        {
            if (exp == null)
                log.Info("#" + msg);
            else
                log.Info("#" + msg + "  " + exp.ToString());
        }


        public static void Warn(object msg, Exception exp = null)
        {
            if (exp == null)
                log.Warn("#" + msg);
            else
                log.Warn("#" + msg + "  " + exp.ToString());
        }
    }
    }
