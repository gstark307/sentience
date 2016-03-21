These instructions assume an Ubuntu Linux operating system.  The procedure for other distros may vary slightly.

**1.  Download the toolchain** and uncompress to a directory of your choosing.

> The blackfin tools can be found at:

> http://www.surveyor.com/blackfin/#blackfin4

**2.  Copy the uncompressed files to root**, like this:

```
sudo cp -r opt /
```

![http://sentience.googlegroups.com/web/toolchain_install.png](http://sentience.googlegroups.com/web/toolchain_install.png)

**3.  Add the directories to your path**

```
gedit /home/<username>/.profile
```

> Add the following to the end of the file:

```
PATH=/opt/uClinux/bfin-elf/bin/:$PATH
export PATH

PATH=/opt/uClinux/bfin-uclinux/bin/:$PATH
export PATH
```

If, like me, you get these paths wrong or mis-spelled them log in using the "Gnome failsafe" option at the login screen, then re-edit your _.profile_ file accordingly.

**4.  Install the xmodem file transfer program**

```
sudo apt-get install lrzsz
```

**5.  Grab a copy of the firmware** from

http://code.google.com/p/surveyor-srv1-firmware/

then uncompress it.

**6.**  Within the _blackfin/srv_ directory there are two scripts called _load1_ and _load2_.  **Edit these to the IP address** of the SVS.

The load1 script looks like this:

```
trap "" 2
echo type X [enter], then control-C, to start XMODEM file transfer of srv1.ldr:
nc 169.254.0.10 10001
sleep 1
sx -Xkbv --tcp-client 169.254.0.10:10001 srv1.ldr
sleep 1
echo
echo make certain that file transfer successfully completed, then
echo type zZ [enter] to write srv1-c.ldr to the flash boot sector:
nc 169.254.0.10 10001
```

Notice that the trap command is used to prevent the script from terminating when CTRL-C is pressed.  The _sx_ command may also vary slightly between distos.

**7.**  Open a terminal and cd to the directory where you extracted the firmware source code.  Then type:

```
cd blackfin/srv

make clean

make
```

If all is well in the world the compilation should complete successfully, with something like the following:

```
Creating LDR srv1.ldr ...
Adding DXE 'srv1.bin' ... 
[initcode 208] [jump block] 
[ELF block: 33976 @ 0x00000000] 
[ELF block:     16428 @ 0xFF800000] 
[ELF block: 14428 @ 0xFF900000] 
[ELF block: 31104 @ 0xFFA00000] OK!
Done!
```

**8.  Run the load1 and load2 scripts** to upload the firmware.

The output should look something like this:

```
sh load1
type X [enter], then control-C, to start XMODEM file transfer of srv1.ldr:
X
CC^C
connecting to [169.254.0.10] <10001>

Sending srv1.ldr, 639 blocks: Give your local XMODEM receive command now.

Transfer complete

make certain that file transfer successfully completed, then
type zZ [enter] to write srv1-c.ldr to the flash boot sector:
zZ
##zZ boot image write count:    131072
```

The script does not terminate after uploading is completed, so use CTRL-C to quit.

**9.**  Once the firmware on both blackfins has been uploaded, **power cycle the SVS**.

**10.  Check the new firmware date** by running:

```
nc 169.254.0.10 10001
```

then pressing V followed by the enter key.

```
##Version - SRV-1 Blackfin - 18:48:34 - Jun 11 2009 (stereo master)
```