# Bottom Navigation Visual Guide

## Mobile View (Portrait Mode)

```
┌─────────────────────────────────┐
│  PoFunQuiz         ☰           │  <- Top Bar (existing)
├─────────────────────────────────┤
│                                 │
│                                 │
│       Page Content              │
│       (Home, Game,              │
│        Leaderboard, etc)        │
│                                 │
│                                 │
│                                 │
│                                 │
│                                 │  <- Content has 80px bottom padding
├─────────────────────────────────┤
│  🏠    📊    🎓    ⚙️          │  <- Bottom Nav (NEW)
│ Home  Stats Practice Settings   │
│                                 │
└─────────────────────────────────┘
     iPhone Safe Area Support
```

## Desktop View (>768px)

```
┌────────────────────────────────────────┐
│  PoFunQuiz              ☰             │  <- Top Bar Only
├────────────────────────────────────────┤
│                                        │
│                                        │
│         Page Content                   │
│         (Full Width)                   │
│                                        │
│                                        │
│                                        │
│                                        │
│                                        │
│                                        │
└────────────────────────────────────────┘
         No Bottom Navigation
```

## Navigation States

### Inactive Item
```
┌──────────┐
│    🏠    │  <- Gray icon
│   Home   │  <- Gray text
└──────────┘
```

### Active Item (Current Page)
```
┌──────────┐
│    🏠    │  <- Blue icon
│   Home   │  <- Blue text
└──────────┘
  Light blue background
```

### Hover/Tap State
```
┌──────────┐
│    🏠    │  <- Slightly darker background
│   Home   │  <- Smooth transition
└──────────┘
```

## Thumb Zone Optimization

```
Phone Screen (Portrait)
┌─────────────┐
│             │ <- Hard to reach (top)
│             │
│             │
│   CONTENT   │
│             │
│             │
│             │ <- Easy reach zone
├─────────────┤
│  🏠 📊 🎓 ⚙️ │ <- Perfect thumb position!
└─────────────┘
```

## Dark Mode

```
Light Mode:
- Background: White (#FFFFFF)
- Icons: Gray (#666) / Blue (#1b6ec2) when active
- Shadow: Black with 10% opacity

Dark Mode:
- Background: Dark Gray (#1a1a1a)
- Icons: Light Gray (#ccc) / Light Blue (#6ab7ff) when active
- Shadow: White with 10% opacity
```

## Touch Target Zones

```
Minimum Safe Area: 44x44px (iOS/Android guideline)

┌────────────────────────┐
│                        │
│     ┌───────────┐      │
│     │  🏠 Icon  │ 44px │ <- Actual touch target
│     │   Home    │      │
│     └───────────┘      │
│         44px           │
└────────────────────────┘
```

## Routes Implemented

| Icon | Label | Route | Status |
|------|-------|-------|--------|
| 🏠 home | Home | `/` | ✅ Working |
| 📊 leaderboard | Stats | `/leaderboard` | ✅ Working |
| 🎓 school | Practice | `/practice` | 🚧 Placeholder |
| ⚙️ settings | Settings | `/settings` | 🚧 Placeholder |

## CSS Media Query Breakpoints

```css
/* Desktop - Bottom nav hidden */
@media (min-width: 769px) {
    .bottom-nav { display: none; }
}

/* Mobile/Tablet - Bottom nav visible */
@media (max-width: 768px) {
    .bottom-nav { 
        display: flex;
        position: fixed;
        bottom: 0;
    }
}
```

## iOS Safe Area Support

```
iPhone X+ Models:
┌─────────────────────┐
│                     │
│     Content         │
│                     │
├─────────────────────┤
│  🏠 📊 🎓 ⚙️       │ <- Nav bar
│                     │ <- Safe area padding
└─────────────────────┘
   Home indicator

CSS: padding-bottom: env(safe-area-inset-bottom);
```

## Animation & Transitions

```css
/* Smooth hover effect */
transition: all 0.2s ease;

/* Active state fade-in */
background-color: rgba(27, 110, 194, 0.08);

/* No tap highlight flash */
-webkit-tap-highlight-color: transparent;
```

## Accessibility Features

✅ Semantic HTML (nav element)
✅ Minimum touch targets (44x44px)
✅ High contrast ratios (4.5:1+)
✅ Color + text labels (not color-only)
✅ Keyboard navigation support (via NavLink)
✅ Screen reader friendly (Material Icons have aria-hidden by default)

## Performance Considerations

- **CSS-only responsive design** - No JavaScript overhead
- **CDN-hosted icons** - Cached across sites
- **Fixed positioning** - GPU-accelerated
- **No layout reflow** - Content padding prevents shifts
- **Minimal CSS** - ~100 lines total

## Future Enhancements (Not in v1)

1. 🔔 **Badge Notifications** - Red dot on Stats icon
2. 🎬 **Page Transitions** - Slide animations between routes
3. 📳 **Haptic Feedback** - Vibrate on tap
4. 🎨 **Custom Themes** - User-selectable accent colors
5. ↔️ **Swipe Gestures** - Horizontal swipe to change tabs
