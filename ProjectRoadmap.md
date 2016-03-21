  * Add methods to allow insertion of simulated structures into occupancy grids, such as walls and doorways.  Grids created in this way can then be used for unit testing.

  * Add routines for probing the grid to recover ranges or an array of probabilities.

  * Unit tests for grid based localization (in simulation)

  * Devise a mechanism whereby the mapping service can ask the odometry server for data, such as "what was my best pose estimate at time t".

  * Odometry fusion.  Odometry offsets need to be applied relative to the current or estimated pose.

  * Simulation unit tests for odometry fusion - prove that pose error remains stable.

  * Data gathering runs in a real environment (not simulation).  Collect wheel and visual odometry data, which may be used for simulation tests.

  * Localizing runs in a real environment, using MCL, or similar.

  * Devise a front end to make using the robot easier.  This might be GWT based (used through a browser) or could be based on GTK/Winforms.  A web based system would be preferable, although has limitations in terms of the kinds of visualizations possible.

  * Keep testing until it works.

  * Create a web site for GROK2, and upload testing data so that the results may be independently analyzed/reproduced (trying to make robotics a real science!).