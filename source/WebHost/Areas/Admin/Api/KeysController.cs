﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Thinktecture.AuthorizationServer.Interfaces;
using Thinktecture.AuthorizationServer.Models;
using Thinktecture.AuthorizationServer.WebHost.Areas.Admin.Models;

namespace Thinktecture.AuthorizationServer.WebHost.Areas.Admin.Api
{
    public class KeysController : ApiController
    {
        IAuthorizationServerAdministration config;

        public KeysController(IAuthorizationServerAdministration config)
        {
            this.config = config;
        }

        public HttpResponseMessage Get()
        {
            var query =
                from item in config.Keys.All.ToArray()
                select new { 
                    item.ID, 
                    item.Name, 
                    type = (item is X509CertificateReference ? "X509" : "Symmetric"), 
                    applicationCount=item.Applications.Count };
            return Request.CreateResponse(HttpStatusCode.OK, query.ToArray());
        }
        
        public HttpResponseMessage Delete(int id)
        {
            var item = this.config.Keys.All.SingleOrDefault(x => x.ID == id);
            if (item != null)
            {
                this.config.Keys.Remove(item);
                this.config.SaveChanges();
            }
            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

       
    }
}
