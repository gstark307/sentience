/*
    reads and writes Windows BMP format images
    Copyright (C) 2009 Bob Mottram
    fuzzgun@gmail.com

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public License
    as published by the Free Software Foundation; either version 2.1 of
    the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston,
    MA 02111-1307 USA
*/

#include "bitmap.h"

Bitmap::Bitmap()
{
    Data = NULL;
    Width = 0;
    Height = 0;
    bytes_per_pixel = 0;
}

Bitmap::Bitmap(int Width, int Height)
{
    Data = NULL;
    bytes_per_pixel = 0;
    Allocate(Width, Height);
}

Bitmap::Bitmap(unsigned char *bmp, int Width, int Height, int BytesPerPixel)
{
    Data = NULL;
    bytes_per_pixel = 0;
    Allocate(Width, Height);

    // colour image
    if (BytesPerPixel == 3)
        memcpy(Data, bmp, Width * Height * 3);

    // mono image
    if (BytesPerPixel == 1)
    {
        int n = 0;
        for (int i = 0; i < Width * Height * 3; i += 3, n++)
        {
            for (int col = 2; col >= 0; col--)
                Data[i + col] = bmp[n];
        }
    }
}


Bitmap::~Bitmap()
{
    FreeMemory();
}

Bitmap::Bitmap(Bitmap &B)
{
    Data = NULL;
    bytes_per_pixel = 0;
    Allocate(B.Width, B.Height);
    memcpy(Data, B.Data, Width * Height * 3);
}

Bitmap::Bitmap(Bitmap &B, int x, int y, int Width, int Height)
{
    Data = NULL;
    bytes_per_pixel = 0;
    Allocate(Width, Height);
    memcpy(Data, B.Data, Width * Height * 3);
}

Bitmap& Bitmap::operator = (const Bitmap &B)
{
    Allocate(B.Width, B.Height);
    memcpy(Data, B.Data, Width * Height * 3);
    return *this;
}

void Bitmap::FreeMemory()
{
    if(Data != NULL)
        delete[] Data;

    Data = NULL;
    Width = 0;
    Height = 0;
}

void Bitmap::Allocate(int Width, int Height)
{
    FreeMemory();
    this->Width = Width;
    this->Height = Height;
    Data = new unsigned char[Width * Height * 3];
    assert(Data != NULL);
}

void Bitmap::Save(const char *filename)
{
    BITMAPFILEHEADER    bmfh;  //stores information about the file format
    BITMAPINFOHEADER    bmih;  //stores information about the bitmap
    std::FILE           *file; //stores file pointer

    //create bitmap file header
    ((unsigned char *)&bmfh.bfType)[0] = 'B';
    ((unsigned char *)&bmfh.bfType)[1] = 'M';
    bmfh.bfSize = 54 + Height * Width * 4;
    bmfh.bfReserved1 = 0;
    bmfh.bfReserved2 = 0;
    bmfh.bfOffBits = 54;

    //create bitmap information header
    //bmih.biSize = 40;
    bmih.biWidth = Width;
    bmih.biHeight = Height;
    bmih.biPlanes = 1;
    bmih.biBitCount = 32;
    bmih.biCompression = 0;
    bmih.biSizeImage = 0;
    bmih.biXPelsPerMeter = 3800;
    bmih.biYPelsPerMeter = 3800;
    bmih.biClrUsed = 0;
    bmih.biClrImportant = 0;

    unsigned char *Data2 = new unsigned char[Width * Height * 4];
    assert(Data2 != NULL);
    int n = 0;
    for (int i = 0; i < Width * Height * 4; i += 4, n += 3)
    {
        Data2[i] = Data[n];
        Data2[i+1] = Data[n+1];
        Data2[i+2] = Data[n+2];
    }

    //save all header and bitmap information into file
    file = fopen(filename, "wb");
    assert(file != NULL);
    fwrite(&bmfh, sizeof(BITMAPFILEHEADER), 1, file);
    fwrite(&bmih, sizeof(BITMAPINFOHEADER), 1, file);
    fwrite(Data2, 4, Width * Height, file);
    fclose(file);

    delete[] Data2;
}

/*!
 * \brief loads a bitmap image from file (see http://local.wasp.uwa.edu.au/~pbourke/dataformats/bmp/)
 * \param filename bitmap file name
 * \return true if the file was loaded correctly
 */
bool Bitmap::FromFile(std::string filename)
{
	bool loaded = false;
    unsigned int uival;
    unsigned short usval, bpp;
    unsigned int size, compression;
    int header_size;
    ifstream inf;

    inf.open(filename.c_str(), ios::binary);
    if (!inf.good())
    {
        cout << "File not found: " << filename << endl;
    }
    else
    {
		if (Data != NULL) delete[] Data;

		// Move to header size position
		inf.seekg( 14 );
		inf.read( (char *) &header_size, 4);

		if (header_size != 40)
		{
			cout << "Malformed bitmap header " << header_size << " bytes (should be 40)" << endl;
		}
		else
		{
			inf.read( (char *) &uival, 4);
			//cout << "width = " << uival << endl;
			Width = uival;
			inf.read( (char *) &uival, 4);
			//cout << "height = " << uival << endl;
			Height = uival;

			//printf("dimensions: %d x %d\n", Width, Height);

			// read plane number
			inf.read((char *) &usval, 2);
			if (usval != 1)
			{
				cout << "Invalid plane number" << endl;
			}
			else
			{
				// read bpp number
				inf.read((char *) &bpp, 2);
				if ((bpp != 32) && (bpp != 24) && (bpp != 8))
				{
					cout << "Invalid number of bits per pixel" << endl;
				}
				else
				{
					bytes_per_pixel = bpp / 8;

					inf.read( (char *) &uival, 4);
					compression = uival;

					if (compression != 0)
					{
						switch(compression)
						{
							case 1:
							{
								cout << "8 bit run length encoded bitmaps are not supported" << endl;
								break;
							}
							case 2:
							{
								cout << "4 bit run length encoded bitmaps are not supported" << endl;
								break;
							}
							case 3:
							{
								cout << "Bitmaps with masks are not supported" << endl;
								break;
							}
						}

					}
					else
					{
						// calculate stride length using this peculiar formula
						int stride = ((Width * bpp + 31) & ~31) >> 3;

						// allocate buffer
						size = Height * stride;
						Data = new unsigned char[size];
						assert(Data != NULL);

						inf.seekg(0, std::ios::end);
						int file_length_bytes = inf.tellg();

						// Skip to the end end of the header
						inf.seekg( file_length_bytes - size, std::ios::beg );

						// Read values
						inf.read((char*) Data , size);

						if (bpp == 24)
						{
							for (int i = 0; i < (int)size; i += bytes_per_pixel)
							{
								// bgr -> rgb
								int temp = Data[i];
								Data[i] = Data[i+2];
								Data[i+2] = temp;
							}
						}

						if (bpp == 32)
						{
							unsigned char *new_Data = new unsigned char[Width * Height * 3];
							assert(new_Data != NULL);
							int n = 0;
							for (int i = 0; i < (int)size; i += 4)
							{
								// bgr -> rgb
								new_Data[n++] = Data[i+2];
								new_Data[n++] = Data[i+1];
								new_Data[n++] = Data[i];
							}
							delete[] Data;
							Data = new_Data;
							bytes_per_pixel = 3;
							stride = Width * 3;
						}

						//flip
						unsigned char *temp_data = new unsigned char[Height * stride];
						assert(temp_data != NULL);
						memcpy(temp_data, Data, Width * Height * bytes_per_pixel);
						int n = 0;
						int n2 = 0;

						for (int y  = 0; y < Height; y++)
						{
							n2 = (Height - 1 - y) * stride;
							for (int x = 0; x < Width; x++, n += bytes_per_pixel, n2 += bytes_per_pixel)
							{
								for (int col = 0; col < bytes_per_pixel; col++)
									Data[n + col] = temp_data[n2 + col];
							}
						}
						delete[] temp_data;

						loaded = true;
					}
				}
			}
		}

		inf.close();
    }
    return(loaded);
}

void Bitmap::SavePPM(const char *filename)
{
    //PPM is a very simple ASCII format
    std::ofstream file(filename);
    file << "P3" << endl;
    file << "# PPM saved to " << filename << endl;
    file << Width << ' ' << Height << endl;
    file << 255 << endl;    //maximum value of a component

    int n = 0;
    for(int y = 0; y < Height; y++)
    {
        for(int x = 0; x < Width; x++)
        {
            file << int(Data[n]) << ' ' << int(Data[n+1]) << ' ' << int(Data[n+2]) << endl;
            n += 3;
        }
    }
}

void Bitmap::Clear()
{
    memset(Data, 0, Width * Height * 3);
}
