cd /usr/bin
sudo mkdir sentience
sudo mkdir sentience/surveyorstereo
sudo mkdir sentience/surveyorstereo/DEBIAN
cd sentience/surveyorstereo
sudo cp ~/develop/sentience/applications/surveyor/surveyorstereo/DEBIAN/control /usr/bin/sentience/surveyorstereo/DEBIAN
sudo cp ~/develop/sentience/applications/surveyor/surveyorstereo/DEBIAN/install /usr/bin/sentience/surveyorstereo/DEBIAN
sudo cp ~/develop/sentience/applications/surveyor/surveyorstereo/DEBIAN/copyright /usr/bin/sentience/surveyorstereo/DEBIAN
sudo cp ~/develop/sentience/applications/surveyor/surveyorstereo/bin/Debug/*.* /usr/bin/sentience/surveyorstereo
cd ~/develop/sentience/applications/surveyor/surveyorstereo/DEBIAN
dpkg -b /usr/bin/sentience/surveyorstereo surveyorstereo.deb
