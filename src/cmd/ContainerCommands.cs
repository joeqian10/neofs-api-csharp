﻿using System;
using System.Threading.Tasks;
using Grpc.Core;
using NeoFS.API.Container;
using NeoFS.API.State;
using NeoFS.Crypto;
using NeoFS.Utils;
using Netmap;

namespace cmd
{
    partial class Program
    {

        static async Task ContainerPut(ContainerPutOptions opts)
        {
            var key = privateKey.FromHex().LoadKey();

            var channel = new Channel(opts.Host, ChannelCredentials.Insecure);

            channel.UsedHost().GetHealth(SingleForwardedTTL, key, opts.Debug).Say();

            uint basicACL = 0;

            switch (opts.BasicACL)
            {
                case "public":
                    basicACL = Container.PublicBasicACL;
                    break;
                case "private":
                    basicACL = Container.PrivateBasicACL;
                    break;
                case "readonly":
                    basicACL = Container.ReadOnlyBasicACL;
                    break;
                default:
                    basicACL = Convert.ToUInt32(opts.BasicACL, 16);
                    break;
            }

            var res = channel.PutContainer(opts.Size, basicACL, SingleForwardedTTL, key, opts.Debug);

            Console.WriteLine();
            Console.WriteLine("Wait for container: {0}", res.CID.ToCID());
            Console.WriteLine();


            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500));

                try
                {
                    var get = channel.GetContainer(res.CID, SingleForwardedTTL, key, opts.Debug);

                    Console.WriteLine("\n\nDone: \n");

                    get.Say();

                    return;
                }
                catch (Exception err)
                {
                    Console.WriteLine("Not ready: {0}", err.Message);
                }
            }

            Console.WriteLine("\nCould not wait for container creation...");
        }

        static async Task ContainerList(ContainerListOptions opts)
        {
            var key = privateKey.FromHex().LoadKey();

            var channel = new Channel(opts.Host, ChannelCredentials.Insecure);

            channel.UsedHost().GetHealth(SingleForwardedTTL, key, opts.Debug).Say();

            var res = channel.ListContainers(SingleForwardedTTL, key, opts.Debug);

            Console.WriteLine("\nUser [{0}] containers: \n", key.ToAddress());

            foreach (var item in res.CID)
            {
                Console.WriteLine("CID = {0}", item.ToCID());
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        static async Task ContainerGet(ContainerGetOptions opts)
        {
            var key = privateKey.FromHex().LoadKey();

            var channel = new Channel(opts.Host, ChannelCredentials.Insecure);

            channel.UsedHost().GetHealth(SingleForwardedTTL, key, opts.Debug).Say();

            byte[] cid;

            try
            {
                cid = Base58.Decode(opts.CID);
            }
            catch (Exception err)
            {
                Console.WriteLine("wrong cid format: {0}", err.Message);
                return;
            }

            var res = channel.GetContainer(cid, SingleForwardedTTL, key, opts.Debug);

            Console.WriteLine();
            Console.WriteLine("Container options:");
            Console.WriteLine("CID = {0}", opts.CID);
            Console.WriteLine("Salt = {0}", res.Container.Salt.ToHex());
            Console.WriteLine("Capacity = {0}", res.Container.Capacity);
            Console.WriteLine("OwnerID = {0}", res.Container.OwnerID.ToAddress());
            Console.WriteLine("Rules = {0}", res.Container.Rules.Stringify());
            Console.WriteLine("ACL = {0}", res.Container.BasicACL.ToString("X"));

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }
}
