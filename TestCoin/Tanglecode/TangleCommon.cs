using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangleToken.Tanglecode
{
    class TangleCommon
    {
        public static String FindPath(int port)
        {
            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Tangle");
            }
            catch (Exception e)
            {

            }

            try
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory().ToString() + "\\Tangle\\" + port);
            }
            catch (Exception e)
            {

            }

            String path = Directory.GetCurrentDirectory().ToString() + "\\Tangle\\" + port + "\\Tangle.tcn";
            return path;
        }
    }
}
