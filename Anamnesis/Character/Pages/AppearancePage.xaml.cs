﻿// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Character.Pages
{
	using System;
	using System.IO;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using Anamnesis.Character.Utilities;
	using Anamnesis.Character.Views;
	using Anamnesis.Files;
	using Anamnesis.GameData;
	using Anamnesis.Memory;
	using Anamnesis.Services;
	using Anamnesis.Styles.Drawers;
	using PropertyChanged;
	using Serilog;

	/// <summary>
	/// Interaction logic for AppearancePage.xaml.
	/// </summary>
	[AddINotifyPropertyChangedInterface]
	public partial class AppearancePage : UserControl
	{
		private static DirectoryInfo? lastLoadDir;
		private static DirectoryInfo? lastSaveDir;

		public AppearancePage()
		{
			this.InitializeComponent();

			this.ContentArea.DataContext = this;
		}

		public GposeService GPoseService => GposeService.Instance;
		public ActorViewModel? Actor { get; private set; }

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.OnActorChanged(this.DataContext as ActorViewModel);
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (!this.IsVisible)
				return;

			this.OnActorChanged(this.DataContext as ActorViewModel);
		}

		private void OnClearClicked(object sender, RoutedEventArgs e)
		{
			this.Actor?.SetMemoryMode(MemoryModes.Write);
			this.Actor?.Equipment?.Arms?.Clear();
			this.Actor?.Equipment?.Chest?.Clear();
			this.Actor?.Equipment?.Ear?.Clear();
			this.Actor?.Equipment?.Feet?.Clear();
			this.Actor?.Equipment?.Head?.Clear();
			this.Actor?.Equipment?.Legs?.Clear();
			this.Actor?.Equipment?.LFinger?.Clear();
			this.Actor?.Equipment?.Neck?.Clear();
			this.Actor?.Equipment?.RFinger?.Clear();
			this.Actor?.Equipment?.Wrist?.Clear();

			this.Actor?.ModelObject?.Weapons?.Hide();
			this.Actor?.ModelObject?.Weapons?.SubModel?.Hide();
			this.Actor?.SetMemoryMode(MemoryModes.ReadWrite);
		}

		private void OnNpcSmallclothesClicked(object sender, RoutedEventArgs e)
		{
			this.Actor?.SetMemoryMode(MemoryModes.Write);
			this.Actor?.Equipment?.Ear?.Clear();
			this.Actor?.Equipment?.Head?.Clear();
			this.Actor?.Equipment?.LFinger?.Clear();
			this.Actor?.Equipment?.Neck?.Clear();
			this.Actor?.Equipment?.RFinger?.Clear();
			this.Actor?.Equipment?.Wrist?.Clear();
			this.Actor?.Equipment?.Arms?.Equip(ItemUtility.NpcBodyItem);
			this.Actor?.Equipment?.Chest?.Equip(ItemUtility.NpcBodyItem);
			this.Actor?.Equipment?.Legs?.Equip(ItemUtility.NpcBodyItem);
			this.Actor?.Equipment?.Feet?.Equip(ItemUtility.NpcBodyItem);
			this.Actor?.SetMemoryMode(MemoryModes.ReadWrite);
		}

		private async void OnLoadClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.All);
		}

		private async void OnLoadEquipmentClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.Equipment);
		}

		private async void OnLoadGearClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.EquipmentGear);
		}

		private async void OnLoadAccessoriesClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.EquipmentAccessories);
		}

		private async void OnLoadAppearanceClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.Appearance);
		}

		private async void OnLoadWeaponsClicked(object sender, RoutedEventArgs e)
		{
			await this.Load(CharacterFile.SaveModes.EquipmentWeapons);
		}

		private void OnLoadNpcClicked(object sender, RoutedEventArgs e)
		{
			this.LoadNpc(CharacterFile.SaveModes.All);
		}

		private void OnLoadNpcEquipmentClicked(object sender, RoutedEventArgs e)
		{
			this.LoadNpc(CharacterFile.SaveModes.Equipment);
		}

		private void OnLoadNpcAppearanceClicked(object sender, RoutedEventArgs e)
		{
			this.LoadNpc(CharacterFile.SaveModes.Appearance);
		}

		private void OnLoadNpcWeaponsClicked(object sender, RoutedEventArgs e)
		{
			this.LoadNpc(CharacterFile.SaveModes.EquipmentWeapons);
		}

		private void LoadNpc(CharacterFile.SaveModes mode)
		{
			SelectorDrawer.Show<NpcSelector, INpcResident>(null, (npc) =>
			{
				if (npc == null)
					return;

				Task.Run(() => this.ApplyNpc(npc, mode));
			});
		}

		private async Task ApplyNpc(INpcResident? npc, CharacterFile.SaveModes mode = CharacterFile.SaveModes.All)
		{
			if (this.Actor == null || npc == null)
				return;

			if (npc.Appearance == null)
				return;

			CharacterFile apFile = npc.Appearance.ToFile();
			await apFile.Apply(this.Actor, mode);
		}

		private async Task Load(CharacterFile.SaveModes mode)
		{
			if (this.Actor == null)
				return;

			try
			{
				OpenResult result = await FileService.Open<LegacyCharacterFile, DatCharacterFile, CharacterFile>(
					lastLoadDir,
					FileService.DefaultCharacterDirectory,
					FileService.FFxivDatCharacterDirectory,
					FileService.CMToolSaveDir);

				if (result.File == null)
					return;

				lastLoadDir = result.Directory;

				if (result.File is LegacyCharacterFile legacyFile)
					result.File = legacyFile.Upgrade();

				if (result.File is DatCharacterFile datFile)
					result.File = datFile.Upgrade();

				if (result.File is CharacterFile apFile)
				{
					await apFile.Apply(this.Actor, mode);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load appearance");
			}
		}

		private async void OnSaveClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				await this.Save();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to save appearance");
			}
		}

		private async Task Save()
		{
			if (this.Actor == null)
				return;

			lastSaveDir = await CharacterFile.Save(lastSaveDir, this.Actor);
		}

		private void OnActorChanged(ActorViewModel? actor)
		{
			this.Actor = actor;

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				bool hasValidSelection = actor != null && (actor.ObjectKind == ActorTypes.Player || actor.ObjectKind == ActorTypes.BattleNpc || actor.ObjectKind == ActorTypes.EventNpc);
				this.IsEnabled = hasValidSelection;
			});
		}
	}
}
