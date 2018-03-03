﻿using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NodaTime;
using OneTimeSecret.Web.Models;
using OneTimeSecret.Web.Services;
using StackExchange.Redis;

namespace OneTimeSecret.Web.Controllers
{
    public class SecretController : Controller
    {
        private readonly ICryptoService cryptoService;
        private readonly IDatabase redis;
        private readonly IClock clock;

        public SecretController(ICryptoService cryptoService, IDatabase redis, IClock clock)
        {
            this.cryptoService = cryptoService;
            this.redis = redis;
            this.clock = clock;
        }

        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost("secret")]
        public IActionResult CreateSecret(CreateSecretViewModel createSecretViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                this.View("Index", createSecretViewModel);
            }

            var key = this.cryptoService.CreateRandomString(24);
            var encryptedData = this.cryptoService.EncryptData(createSecretViewModel.Secret, createSecretViewModel.Passphrase);
            var expiry = TimeSpan.FromSeconds(createSecretViewModel.TTL);
            var expiryInstant = this.clock.GetCurrentInstant().Plus(Duration.FromTimeSpan(expiry));

            RedisModel model = new RedisModel
            {
                EncryptedData = encryptedData,
                HasPassphrase = createSecretViewModel.Passphrase != null
            };

            var redisString = JsonConvert.SerializeObject(model);

            this.redis.StringSet(key, redisString, expiry, When.Always);

            var fullAccessUrl = this.Url.Action("ShowSecret", "Secret", new { id = key }, this.Request.Scheme, this.Request.Host.Value);
            var burnUrl = this.Url.Action("BurnSecret", "Secret", new { id = key }, this.Request.Scheme, this.Request.Host.Value);
            var created = new CreatedSecretViewModel
            {
                AccessUrl = fullAccessUrl,
                BurnUrl = burnUrl,
                PassphraseRequired = string.IsNullOrEmpty(createSecretViewModel.Passphrase),
                Expires = expiryInstant,
                DurationString = this.DurationString(expiry)
            };

            return this.View(created);
        }

        [HttpGet("secret/{id}")]
        public IActionResult ShowSecret(string id)
        {
            var vm = new ShowSecretViewModel();

            string data = this.redis.StringGet(id);
            if (data != null)
            {
                var model = JsonConvert.DeserializeObject<RedisModel>(data);
                vm.HasPassphrase = model.HasPassphrase;
            }

            return this.View(vm);
        }

        [HttpPost("secret/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult ShowSecretConfirmed(string id)
        {
            string data = this.redis.StringGet(id);
            if (data == null)
            {
                return this.View(new ShowSecretViewModel
                {
                    DoesntExist = true
                });
            }

            var model = JsonConvert.DeserializeObject<RedisModel>(data);
            ShowSecretViewModel vm = new ShowSecretViewModel
            {
                HasPassphrase = model.HasPassphrase
            };

            try
            {
                var passphrase = this.Request.Form["passphrase"];

                string secret = this.cryptoService.DecryptData(model.EncryptedData, passphrase);
                
                // always delete before showing to prevent it from being seen twice
                this.redis.KeyDelete(id);

                vm.Secret = secret;
                return this.View(vm);
            }
            catch (Exception)
            {
                if (model.HasPassphrase)
                {
                    vm.DidError = true;
                    return this.View("ShowSecret", vm);
                }
                else
                {
                    throw;
                }
            }




        }

        [HttpGet("secret/{id}/burn")]
        public IActionResult BurnSecret(string id)
        {
            return this.View();
        }

        [HttpPost("secret/{id}/burn")]
        [ValidateAntiForgeryToken]
        public IActionResult BurnSecretConfirmed(string id)
        {
            this.redis.KeyDelete(id);
            BurnSecretConfirmedViewModel vm = new BurnSecretConfirmedViewModel()
            {
                BurnedAt = this.clock.GetCurrentInstant()
            };

            return this.View(vm);
        }


        private string DurationString(TimeSpan expiry)
        {
            if (expiry.Days > 1)
            {
                return $"{expiry.Days} days";
            }
            else if (expiry.Days == 1)
            {
                return "1 day";
            }
            else if (expiry.Hours > 1)
            {
                return $"{expiry.Hours} hours";
            }
            else if (expiry.Hours == 1)
            {
                return "1 hour";
            }
            else
            {
                return $"{expiry.Minutes} minutes";
            }
        }
    }
}
