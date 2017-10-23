﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
//#define TEST_VANILLA
using System;
using System.Net;
using ClassicalSharp.Entities;
using ClassicalSharp.Generator;
using ClassicalSharp.Gui.Screens;
using ClassicalSharp.Physics;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp.Singleplayer {

	public sealed class SinglePlayerServer : IServerConnection {
		
		internal PhysicsBase physics;
		
		public SinglePlayerServer(Game window) {
			game = window;
			physics = new PhysicsBase(game);
			SupportsFullCP437 = !game.ClassicMode;
			SupportsPartialMessages = true;
			IsSinglePlayer = true;
		}
		
		public override void Connect(IPAddress address, int port) {
			game.Chat.SetLogName("Singleplayer");
			game.UseCPEBlocks = game.UseCPE;
			int max = game.UseCPEBlocks ? Block.MaxCpeBlock : Block.MaxOriginalBlock;
			for (int i = 1; i <= max; i++) {
				BlockInfo.CanPlace[i] = true;
				BlockInfo.CanDelete[i] = true;
			}
			game.AsyncDownloader.DownloadSkin(game.LocalPlayer.SkinName,
			                                  game.LocalPlayer.SkinName);
			
			game.Events.RaiseBlockPermissionsChanged();
			int seed = new Random().Next();
			BeginGeneration(128, 64, 128, seed, new NotchyGenerator());
		}
		
		char lastCol = '\0';
		public override void SendChat(string text, bool partial) {
			if (!String.IsNullOrEmpty(text))
				AddChat(text);
			if (!partial) lastCol = '\0';
		}
		
		void AddChat(string text) {
			text = text.TrimEnd().Replace('%', '&');
			if (!IDrawer2D.IsWhiteCol(lastCol))
				text = "&" + lastCol + text;
			
			char col = IDrawer2D.LastCol(text, text.Length);
			if (col != '\0') lastCol = col;
			game.Chat.Add(text, MessageType.Normal);
		}
		
		public override void SendPosition(Vector3 pos, float rotY, float headX) {
		}
		
		public override void SendPlayerClick(MouseButton button, bool buttonDown, byte targetId, PickedPos pos) {
		}
		
		public override void Dispose() {
			physics.Dispose();
		}
		
		public override void Tick(ScheduledTask task) {
			if (Disconnected) return;
			physics.Tick();
			CheckAsyncResources();
		}
	}
}