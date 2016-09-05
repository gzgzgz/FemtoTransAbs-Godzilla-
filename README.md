# FemtoTransAbs (Godzilla)
<img align="middle" src="spec.jpg" alt="Smiley face" height="400" width="500"><br> </br>
A real time transient absorption CCD spectrometer instrument control/data acquisition software written in C#<br></br>
Some notes:<br> </br>
- 1) This software currently only implements the BWTEK Exemplar LS CCD spectrometer (low level APIs are provided by BWTEK Inc.), will soon support SOL 1.7 IR CCD spectrometer. In future, it may support Ocean Optics CCD spectrometer. <br> </br>
- 2) It currently only supports mechanical delay stage from NewPort Inc. (I am not able to test other brands at the moment)<br></br> 
- 3) It uses pulsed laser external sync signal to trigger the BWTEK CCD spectrometer, and it detects the instrument connection at the beginning.<br> </br>
- 4) Make sure your laser system is a megahertz reptition rate (or at least tens of kilohertz reptition rate) system in order to obtain good signal-to-noise.<br> </br>
- 5) other hardware required: <br>  
   - a) A optical chopper with sync signal output<br>
   - b) A TTL frequency doubler that doubles the input TTL signal frequency, and will need to function in 10-2000 Hz range. (home-built diff op amp circuit will do)<br>
   - c) ultrafast optics to do the transient absorption measurements <br>

Other features: <br> </br>
 The software save csv files for each scan and their average, so that the data format is fully compatible with Surface Xplore suite (Ultrafast System Inc.). <br> </br>
 
For any other questions, please email gzgzgz{\replace_with_at/}gmail.com
