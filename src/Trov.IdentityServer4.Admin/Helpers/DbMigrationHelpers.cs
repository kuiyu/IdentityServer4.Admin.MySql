﻿using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trov.IdentityServer4.Admin.Configuration;
using Trov.IdentityServer4.Admin.Constants;
using Trov.IdentityServer4.EntityFramework.DbContexts;
using Trov.IdentityServer4.EntityFramework.Entities.Identity;

namespace Trov.IdentityServer4.Admin.Helpers
{
    public static class DbMigrationHelpers
    {
        /// <summary>
        /// Generate migrations before running this method, you can use command bellow:
        /// Nuget package manager: Add-Migration DbInit -context AdminDbContext -output Data/Migrations
        /// Dotnet CLI: dotnet ef migrations add DbInit -c AdminDbContext -o Data/Migrations
        /// </summary>
        /// <param name="host"></param>
        public static async Task EnsureSeedData(IWebHost host)
        {
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                await EnsureSeedData(services);
            }
        }

        public static async Task EnsureSeedData(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserIdentity>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserIdentityRole>>();

                var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                context.Database.Migrate();
                configContext.Database.Migrate();
                await EnsureSeedIdentityServerData(configContext);
                await EnsureSeedIdentityData(userManager, roleManager);
            }
        }

        /// <summary>
        /// Generate default admin user / role
        /// </summary>
        private static async Task EnsureSeedIdentityData(UserManager<UserIdentity> userManager,
            RoleManager<UserIdentityRole> roleManager)
        {
            // Create admin role
            if (!await roleManager.RoleExistsAsync(AuthorizationConsts.AdministrationRole))
            {
                var role = new UserIdentityRole { Name = AuthorizationConsts.AdministrationRole };

                await roleManager.CreateAsync(role);
            }

            // Create admin user
            if (await userManager.FindByNameAsync(Users.AdminUserName) != null) return;

            var user = new UserIdentity
            {
                UserName = Users.AdminUserName,
                Email = Users.AdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, Users.AdminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, AuthorizationConsts.AdministrationRole);
            }
        }

        /// <summary>
        /// Generate default clients, identity and api resources
        /// </summary>
        private static async Task EnsureSeedIdentityServerData(ConfigurationDbContext context)
        {
            if (!context.Clients.Any())
            {
                foreach (var client in Clients.GetClients().ToList())
                {
                    await context.Clients.AddAsync(client.ToEntity());
                }

                await context.SaveChangesAsync();
            }

            if (!context.IdentityResources.Any())
            {
                var identityResources = ClientResources.GetIdentityResources().ToList();

                foreach (var resource in identityResources)
                {
                    await context.IdentityResources.AddAsync(resource.ToEntity());
                }

                await context.SaveChangesAsync();
            }

            if (!context.ApiResources.Any())
            {
                foreach (var resource in ClientResources.GetApiResources().ToList())
                {
                    await context.ApiResources.AddAsync(resource.ToEntity());
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
