# Accreditation Validation Web App

This is a Blazor WebAssembly project that provides a user-friendly interface for accreditation validation. The app features a bottom tab menu with localized navigation, responsive UI elements, and integration with a component-based architecture.

## 🚀 Features

- **Bottom Tab Navigation**: Fixed footer menu with icons and labels.
- **Localization Support**: Dynamic language support using `@LocalizationService`.
- **Component-Based**: Uses reusable components like `<AuthWrapper>`.
- **Responsive Design**: Mobile-first layout with flexible styling.
- **Icon Support**: Uses [Bootstrap Icons](https://icons.getbootstrap.com/) for menu items.

---

## 📁 Project Structure

```
/wwwroot
│   └── css/
│       └── site.css                # Contains styles for bottom tab and layout
/Shared
│   └── MainLayout.razor           # Main layout with tab navigation
/Components
│   └── AuthWrapper.razor          # Wraps auth-guarded content
/Pages
│   └── Validation.razor
│   └── Data.razor
│   └── Settings.razor
Program.cs                         # Blazor entry point
App.razor                          # App root component
```

---

## 🎨 Styling

The bottom navigation is styled in `site.css`:

```css
.bottom-tab-menu {
  position: fixed;
  bottom: 0;
  display: flex;
  justify-content: space-around;
  background-color: #fff;
  box-shadow: 0 -2px 5px rgba(0, 0, 0, 0.3);
}

.tab-item {
  display: flex;
  flex-direction: row;
  color: #808080;
  transition: color 0.2s ease;
}

.tab-item:hover,
.tab-item.active {
  color: #407cc9;
}
```

---

## 🛠️ Dependencies

- [.NET 9](https://dotnet.microsoft.com/)
- [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/)
- [Bootstrap Icons](https://icons.getbootstrap.com/)

---

## 🧪 Running the App

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Run the development server:
   ```bash
   dotnet run
   ```

3. Visit `https://localhost:5001` in your browser.

---

## 🌍 Localization

All text in the UI is localized using `@LocalizationService["Key"]`. Make sure your localization resources are defined in `.resx` or `*.json` depending on the setup.

---

---

## 📃 License

MIT License © 2025
