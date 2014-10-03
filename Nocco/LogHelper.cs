using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nocco
{
    public partial class Helpers
    {
        /// <summary>
        /// Writes <paramref name="messages"/> to the console if <see cref="Nocco.BeVerbose"/> is true
        /// </summary>
        /// <param name="message"></param>
        public static void LogMessages(params string[] messages)
        {
            if (!App.Settings.BeVerbose)
                return;

            foreach (string message in messages)
                Console.WriteLine(message);
        }
    }
}
