# Port Assetto Corsa Content Manager to Linux
The task is to port the Content Manager app to Linux.

App is already ported to .NET 10, so it's buildable and working with the included dotnet application.

## Requirements
- Keep ALL features of the original app.
- Keep the UI and functionality as close to the original as possible.
- You can use Avalonia or Eto.Forms for the GUI. Avalonia is preferable, but Eto.Forms may be closer in syntax. Research which one will be better (require less work) before doing anything - take into consideration, that the Content Manager app is heavily using custom controls.
- For custom content manager functionality you MUST use the AcTools.* and/or AcManager.* libraries. You are allowed to modify these, but avoid doing modifications if not necessary.
- App MUST still work on Windows. So target platforms are Linux and Windows.
- For now don't worry about starting the Assetto Corsa game - this will be handled separately. Leave this part as is (modifications limited only to what's necessary for building the project).
- If something is not easily portable to Linux, leave it as is (modifications limited to what's needed for building), but you MUST document these.

## Documentation
- Keep the documentation inside a "LinuxPortDocs.md" file.

## Notes
- You are running inside a dev container as a root user. You are allowed to install packages using apt. But you MUST document every install.
- You have a working wayland session as "wayland-0". You also have a working XWayland session in "DISPLAY=:0". These are running labwc compositor.
- For screenshots you can use the installed "grim" package.
- Container is running without a dbus session nor GPU acceleration, so you SHOULD take this into consideration, as some things may be slower than usual.
- You SHOULD split the work into multiple commits, but you CAN'T push any changes. Even if you try - SSH key is locked with a password, so it's not possible and you MUST NOT even try.
