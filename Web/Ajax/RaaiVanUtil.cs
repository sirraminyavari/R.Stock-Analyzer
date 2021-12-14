using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using RaaiVan.Modules.GlobalUtilities;

namespace RaaiVan.Web.Ajax
{
    public class ParamsContainer
    {
        private HttpContext _Context;

        public ParamsContainer(HttpContext context)
        {
            _Context = context;
        }

        public void return_response(ref string responseText)
        {
            if (_Context == null || _Context.Response == null) return;

            _Context.Response.Clear();
            _Context.Response.ContentType = "text/json";
            _Context.Response.BufferOutput = true;
            _Context.Response.Write(responseText);

            _Context.Response.Flush(); // Sends all currently buffered output to the client.
            _Context.Response.SuppressContent = true;  // Gets or sets a value indicating whether to send HTTP content to the client.
            _Context.ApplicationInstance.CompleteRequest(); // Causes ASP.NET to bypass all events and filtering in the HTTP pipeline chain of execution and directly execute the EndRequest event.

            /*
            _Context.Response.End();
            _Context.Response.Close();
            */
        }

        public void return_response(string responseText)
        {
            return_response(ref responseText);
        }
    }
}