# Key Swapper

A super simple app that helps non-US writers type their special characters by remapping certain keys, while keeping the US keyboard layout.

For example, press <kbd>`</kbd> and get **ë**, or <kbd>Shift</kbd>+<kbd>`</kbd> and get **Ë**, without switching keyboard layouts or memorizing Alt codes. Everything else on your keyboard stays exactly the same.

## Download and install

1. Go to the [**Releases**](https://github.com/FatjonBeqa/key-swapper/releases/latest) page.
2. Download **`KeySwapper.zip`** and unzip it anywhere (for example, your Documents folder).
3. Double-click **`KeySwapper.exe`**.

There is no installer and nothing else to install. The app is a single file.

> **"Windows protected your PC"?** The app isn't code-signed (signing costs money), so Windows SmartScreen may warn you the first time. Click **More info** → **Run anyway**. The full source code is in this repo if you'd like to check what it does.

**Requirements:** Windows 10 or 11 (64-bit).

## How to use it

- **Add a rule:** click **+ Add rule**, press the key you want to swap (with or without Shift), then type the character you want instead and click **Save**.
- **Turn a rule on or off** with its checkbox, or remove it with the delete button next to it.
- **Turn all swapping on or off** at any time with <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>K</kbd>, or from the tray icon menu.
- **Minimize** the window and Key Swapper keeps running in the system tray. Click the tray icon to open it again.
- **Start with Windows** is on by default, so it's always ready. Untick it in the window if you'd rather start it yourself.

Shortcuts that use <kbd>Ctrl</kbd>, <kbd>Alt</kbd> or <kbd>Win</kbd> are never changed, so things like <kbd>Ctrl</kbd>+<kbd>`</kbd> keep working.

The app comes with three ready-made rules, which you can change or remove:

| Press | Types |
|---|---|
| <kbd>`</kbd> | ë |
| <kbd>Shift</kbd>+<kbd>`</kbd> (~) | Ë |
| <kbd>Shift</kbd>+<kbd>\\</kbd> (\|) | ç |

Your rules are saved in `%AppData%\KeySwapper\settings.json`.

## Uninstall

1. Untick **Start with Windows**.
2. Quit the app (close the window and choose **Quit**, or right-click the tray icon → **Exit**).
3. Delete `KeySwapper.exe` and, optionally, the `%AppData%\KeySwapper` folder.

## Build from source

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/FatjonBeqa/key-swapper.git
cd key-swapper/KeySwapper
dotnet run
```

To make a single self-contained `.exe`:

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ../release
```

## License

[MIT](LICENSE). Free to use, change and share.
