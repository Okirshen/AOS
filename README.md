# AOS

Automated Operating System is a mod that lets you program your own flight computers and automate your SFS creations.

## Installation Steps
Download AOS.zip from the latest release and extract in the SFS Mods folder, place your scripts in the scripts folder inside the AOL folder, install [UITools](https://github.com/cucumber-sp/UITools) and you are good to go.

## Creating Scripts
The mod injects the following variables to the lua file:
```
throttle - 0-100 precentage of throttle
angle - angle of the rocket in degrees
altitude - current altitude above terrain in meters
velocity - velocity vector of the rocket
angular_velocity - angular velocity of the rocket
mass - mass of the rocket
turn - turning force from -1 to 1
SAS - boolean deciding if SAS should be enabled
```

and it injects the following functions
```
stage() - seperates the first stage
```

All code placed in the `update(delta)` function will be run on a loop with delta being the time since last frame.

## Running Scripts
In the world scene there is a minimized window, open it, choose your script, and click run to start.