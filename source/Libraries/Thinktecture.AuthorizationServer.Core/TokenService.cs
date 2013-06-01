﻿/*
 * Copyright (c) Dominick Baier, Brock Allen.  All rights reserved.
 * see license.txt
 */

using Microsoft.IdentityModel.Tokens.JWT;
using System;
using System.Collections.Generic;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using Thinktecture.AuthorizationServer.Interfaces;
using Thinktecture.AuthorizationServer.Models;
using Thinktecture.IdentityModel;

namespace Thinktecture.AuthorizationServer
{
    public class TokenService
    {
        GlobalConfiguration globalConfiguration;

        public TokenService(GlobalConfiguration globalConfiguration)
        {
            this.globalConfiguration = globalConfiguration;
        }

        public virtual TokenResponse CreateTokenResponse(TokenHandle handle, ITokenHandleManager handleManager)
        {
            handleManager.Delete(handle.HandleId);

            var resourceOwner = Principal.Create(
                "OAuth2",
                handle.ResourceOwner.ToClaims().ToArray());

            var validatedRequest = new ValidatedRequest
            {
                Client = handle.Client,
                Application = handle.Application,
                Scopes = handle.Scopes
            };

            var response = CreateTokenResponse(validatedRequest, resourceOwner);

            if (handle.CreateRefreshToken)
            {
                var refreshTokenHandle = TokenHandle.CreateRefreshTokenHandle(
                    handle.Client,
                    handle.Application,
                    resourceOwner.Claims,
                    handle.Scopes,
                    handle.RefreshTokenExpiration);

                handleManager.Add(refreshTokenHandle);
                response.RefreshToken = refreshTokenHandle.HandleId;
            }

            return response;
        }

        public virtual TokenResponse CreateTokenResponse(ValidatedRequest request, ClaimsPrincipal resourceOwner)
        {
            try
            {
                var subject = CreateSubject(request, resourceOwner);
                var descriptor = CreateDescriptor(request, subject);
                var token = CreateToken(descriptor);

                return new TokenResponse
                {
                    AccessToken = token,
                    ExpiresIn = request.Application.TokenLifetime * 60,
                    TokenType = "Bearer"
                };
            }
            catch (Exception ex)
            {
                Tracing.Error(ex.ToString());
                throw;
            }
        }

        protected virtual string CreateToken(SecurityTokenDescriptor descriptor)
        {
            var handler = new JWTSecurityTokenHandler();

            var token = handler.CreateToken(descriptor);
            return handler.WriteToken(token);
        }

        protected virtual SecurityTokenDescriptor CreateDescriptor(ValidatedRequest request, ClaimsIdentity subject)
        {
            var descriptor = new SecurityTokenDescriptor
            {
                AppliesToAddress = request.Application.Audience,
                Lifetime = new Lifetime(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(request.Application.TokenLifetime)),
                TokenIssuerName = globalConfiguration.Issuer,
                Subject = subject,
                SigningCredentials = request.Application.SigningCredentials
            };

            return descriptor;
        }

        protected virtual ClaimsIdentity CreateSubject(ValidatedRequest request, ClaimsPrincipal resourceOwner)
        {
            var claims = new List<Claim>();

            claims.AddRange(CreateRequestClaims(request));
            claims.AddRange(CreateResourceOwnerClaims(resourceOwner));

            var subject = new ClaimsIdentity(claims, "tt.authz");
            return subject;
        }

        protected virtual List<Claim> CreateResourceOwnerClaims(ClaimsPrincipal resourceOwner)
        {
            return resourceOwner.Claims.ToList();
        }

        protected virtual List<Claim> CreateRequestClaims(ValidatedRequest request)
        {
            var claims = new List<Claim>
            {
                new Claim("client_id", request.Client.ClientId)
            };

            request.Scopes.ForEach(s => claims.Add(new Claim("scope", s.Name)));

            return claims;
        }
    }
}
