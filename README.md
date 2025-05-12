# 3D Macro Supermarket Floorplan Planner
## User Guide

This guide provides comprehensive instructions for using the 3D Supermarket Floor Planning Tool, which allows you to design and visualize supermarket layouts in a 3D environment.

## Table of Contents
1. [Getting Started](#getting-started)
2. [Camera Controls](#camera-controls)
3. [Creating the Floor](#creating-the-floor)
4. [Component Placement](#component-placement)
5. [Grid Controls](#grid-controls)
6. [Unique Features](#unique-features)
7. [Code Structure](#code-structure)
8. [Project Access](#project-access)

## Getting Started

When you first launch the application, you'll be prompted to create a new floor plan. This is your canvas for designing the supermarket layout.

## Camera Controls

Navigate through your supermarket design using these camera controls:

| Action | Control |
|--------|---------|
| Move Camera | **WASD** or **Arrow Keys** |
| Rotate Camera | **Q** (counter-clockwise) and **E** (clockwise) |
| Zoom In/Out | **Mouse Scroll Wheel** |

**Note**: Camera movements will be smooth with acceleration and deceleration. The camera is also restricted to stay within the boundaries of your floor plan.

## Creating the Floor

1. When you start the application, you'll see a floor creation panel
2. Enter the width and length of your supermarket floor in meters
3. Click "Create Floor" to generate the floor and surrounding walls
4. The camera will automatically position to give you a good view of the entire floor

## Component Placement

### Selecting Components

1. Open the side menu by clicking the tab on the left side of the screen
2. Browse through available components organized by categories:
   - Shelves
   - Fridges
   - Checkouts
   - Walls
   - Other components

### Placing Components

1. Click on a component in the menu to select it
2. Move your mouse over the floor to see a preview of the component
3. The preview will show blue when placement is valid or red when invalid
4. **Left-click** to place the component
5. Press **R** to rotate the component before placing
6. **Right-click** or press **Escape** to cancel placement

### Modifying Placed Components

- Click on a placed component to select it
- Use the **Mouse** to move and **R** for rotate the selected component
- Press **Right-click** to remove the selected component

## Grid Controls

The grid helps you align components precisely:

1. Access grid settings in the Settings tab of the side menu
2. Toggle grid visibility on/off
3. Adjust grid cell size using the slider (from 0.1m to 10m)
4. Toggle "Snap to Grid" to enable/disable automatic alignment to grid lines

## Unique Features

### Top-Down View Toggle

The planner includes a convenient top-down view toggle that helps you see your layout from a bird's-eye perspective:

1. Click the **View Toggle** button in the top-left corner of the screen
2. The camera will smoothly transition between the perspective view and top-down view
3. Use top-down view for precise placement and overall layout planning
4. Return to orthographic view for your store

### Adjustable Grid Density

Fine-tune your layout precision with the adjustable grid system:

1. Choose from preset grid sizes (small, medium, large) in the Settings panel
2. Finer grids (higher density) allow for more precise component placement
3. Coarser grids (lower density) are better for rapid layout creation

### Explanation of Original Features

These unique features were specifically designed to address the practical needs of store owners and planners:

**Top-Down View**: Store planning traditionally begins with a bird's-eye view. The toggle between orthogonal and top-down views mimics the natural workflow of professional store designers, allowing users to switch between big-picture planning (top-down) and immersive validation (perspective view). This dual-view approach helps store owners better visualize how their layout decisions.

**Variable Grid System**: By making the grid density adjustable, users can work at different levels of precision as needed. When planning major section divisions, a coarse grid helps with quick, macro-level decisions. When fine-tuning shelving arrangements or checkout areas where space is at a premium, the ability to switch to a finer grid ensures optimal space utilization. This flexibility mirrors how real-world store planning evolves from rough zoning to precise fixture placement.

Both features combine to give store owners professional planning capabilities that adapt to different phases of the design process.

## Code Structure

### Namespace Organization
- **Controllers**: Handles user input and interactions
- **Data**: Contains data models and structures
- **Managers**: Global systems with application-wide scope
- **Services**: Provides specialized functionality
- **UI**: Manages interface elements

### Key Systems
- **Camera System**: Navigation and view switching
- **Component System**: Defines and manages placeable objects
- **Placement System**: Handles object placement and validation
- **Grid System**: Creates visual grid and alignment functionality
- **UI System**: User interface panels and menus

### Design Approach
- Uses singletons for global access (GridManager, ComponentManager)
- Services provide modular, reusable functionality
- Communication happens through events to reduce coupling

This architecture ensures maintainability through separation of concerns and extensibility through modular design.

## Project Access

For additional help or to report issues, please contact the development team.

You can access the project at the following link:
- **Project URL**: [https://liudengmeng.itch.io/3d-macro-supermarket-floorplan-planner](https://liudengmeng.itch.io/3d-macro-supermarket-floorplan-planner)
- **Access Password**: 3DMacro