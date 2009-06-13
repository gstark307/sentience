################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../bitmap.cpp \
../drawing.cpp \
../fileio.cpp \
../main.cpp \
../stereo.cpp 

OBJS += \
./bitmap.o \
./drawing.o \
./fileio.o \
./main.o \
./stereo.o 

CPP_DEPS += \
./bitmap.d \
./drawing.d \
./fileio.d \
./main.d \
./stereo.d 


# Each subdirectory must supply rules for building sources it contributes
%.o: ../%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -O0 -g3 -Wall -c -fmessage-length=0 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@:%.o=%.d)" -o"$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


