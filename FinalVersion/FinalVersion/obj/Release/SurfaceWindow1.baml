   M S B A M L   `   `   `  ����   FinalVersion!    FinalVersion.SurfaceWindow1   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     e aMicrosoft.Surface.Presentation, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e351s)http://schemas.microsoft.com/surface/2008    � Title$
  aPen�� � 
ResizeMode$ NoResize=� � WindowStyle$
 None=�.���� -    ��Name$ myCanvas��
�������$
��1080��$
��1920��$
��1080��$
��1920��.+��� -    ��Name$ dragRectangle��$y�Hidden=�$	��150��$	��150��
 �� Top$	 400�� �� Left$	 550�� K� ZIndex$ 2��
���**�� � Cursor$
 None��}�.  �� -   $ theBox��$y�Hidden=�$	��150��$	��150��$ 1��$	 400��$
 1212��
��� d ��� -   	 ��Name$	 	textBoard��$	 712��$	 265��$�	���The Microsoft Surface® platform brings people together  to connect, learn, and decide. It enables experiences that  change the way people collaborate and connect with a  360-degree interface. And, with PixelSense™, Microsoft Surface sees and responds to touch and real world objects —supporting more than 50 simultaneous inputs. This  experience comes to life in the new 40 inch Samsung  SUR40 for Microsoft Surface that can be used as a table,  on the wall, or embedded in other fixtures or furniture.  PixelSense allows a display to recognize fingers, hands,  and objects placed on the screen, enabling vision-based  interaction without the use of cameras. The individual  pixels in the display see what's touching the screen and  that information is immediately processed and interpreted.  Think of it like the connection between the eye and the  brain. You need both, working together, to see. In this case,  the eye is the sensor in the panel, it picks up the image and it feeds that to the brain which is our vision input  processor that recognizes the image and does something  with it. Taken in whole…this is PixelSense technology.��$��18(�
���   �$	��540��$	��512��$y�Hidden=��� -   $ 
mediumRect��$��28��$	��512��$	 712��$	 580�����	#39000000$y�Hidden=��� -   $ 	shortRect��$	 712��$	 693��$��40��$	��512�����	#39000000$y�Hidden=��� -   $ longRect��$	 717��$	 367��$��20��$	��497�����	#39000000$y�Hidden=��� -   $��permanentLabel1��
���   �$��Andrea Pacino��$
 1558��$	 982��$��40��$	��129���� -	   $��permanentLabel2��
���   �$��University of Pisa��$
 1558��$
 1005���� -
   $��permanentLabel3��
���   �$!��IT-University of Copenhagen��$
 1558��$
 1028���� -   $	 wordDrawLabel��
���   �
�������$	 250��$	 440��$	��983��$	��720��$��PALERMO��$��Center=�$��Center=�$	��200(�$y�Hidden=�>  8Microsoft.Surface.Presentation.Controls.SurfaceInkCanvas  -   
  Name$
 highlightBoard��$	 533��$	 267��$	��597��$	��826��   
Background
 ����    UsesTouchShape .     EditingMode$
 None=� J� Opacity$	 0.5Q�$y�Hidden=�  -   $
 	drawBoard��$	 440��$	 250��$	��720��$	��983��
 ����  .  $
 None=�$y�Hidden=�;  5Microsoft.Surface.Presentation.Controls.SurfaceButton  -     Name$ 
modeButton��$ 20��$ 20��$��
Pen Mode 1��$	��250��$	��100��$��Center=�$��Center=�$��32(�$y�Hidden=�  -   $ 
taskButton��$ 20��$	 139��$��
Task1 - HL��$��32(�$	��100��$��Center=�$��Center=�$	��250��$y�Hidden=�  -   $ highlightButton��$	��200��$��50��$��	Highlight��$	 142��$	 919��$��Center=�$��Center=�$��24(�
�������
���   �$y�Hidden=�  -   $ ClearButton��$	��200��$��50��$��Clear��$
 1517��$	 813��$��Center=�$��Center=�$��24(�
�������
���   �$y�Hidden=�  -   $ difficultyButton��$ 20��$	 266��$
��Easy��$��32(�$	��100��$��Center=�$��Center=�$	��250��$y�Hidden=�  -   $ 
drawButton��
�������$	 142��$	 919��$	��Ink��$��24(�
���   �$��50��$��Center=�$��Center=�$	��200��$y�Hidden=�  -   $ 
doneButton��
�������$
 1517��$	 754��$
��Done��$��24(�
���   �$��50��$��Center=�$��Center=�$	��200��$y�Hidden=�  -   $ selectButton��
�������$	 142��$	 919��$��Select��$��24(�
���   �$��50��$��Center=�$��Center=�$	��200��$y�Hidden=��� -   $	 highlightLabel��$	 567��$	 140��$9��3Please highlight the line with the black background��$��32(�
���   �$y�Hidden=��� -   $	 	DragLabel��$	 400��$	 140��$C��=Please, drag the red square and drop it into the green square��$��32(�
���   �$y�Hidden=��� -   $	 	drawLable��$	 700��$	 140��$"��Please, write on the letters��$��32(�
���   �$y�Hidden=�  -   $ userNameButton��
�������$��Center=�$��Center=�$
 1162��$	 413��$��59��$	��172��$��START��
���   �$��24(�$y�Visible=�<  6Microsoft.Surface.Presentation.Controls.SurfaceTextBox  -     Name$ userTB��$��60��$	��595��$	 566��$	 412��   KeyboardLayout$ Alphanumeric=�$��32(�$y�Visible=��� -   $	 userNameLabel��$	 567��$	 381��$��Insert Name and Surname��$��24(�
���   �$y�Visible=�  -   $��techniqueTB��$
 1072��$	 478��$��55��$ Numeric=�$��38��$��32(� �� 	MaxLength$ 1��$y�Visible=�  -   $��taskTB��$
 1123��$	 478��$��55��$ Numeric=�$��38��$��32(� �� CharacterCasing$ Upper=�$ 1��$y�Visible=�  -   $��groupTB��$	 883��$	 478��$��55��$ Alphanumeric=�$��38��$��32(�$ Upper=�$ 1��$y�Visible=��� -   $��
groupLabel��$y�Visible=�$	 567��$	 479��$ ��Insert group name(A,B,C,D)��$��24(�
���   �  -    $��
nextButton��
�������$	 142��$	 540��$
��NEXT��$��24(�
���   �$��59��$��Center=�$��Center=�$	��172��$y�Hidden=��� -!   $��
nextLabel1��$	 118��$	 353��$��
Well Done!��$��48(�$��68��$	��306��
���   �$y�Hidden=��� -"   $��
nextLabel2��$ 21��$	 433��$L��FTake your time. When you're ready press NEXT to continue the experimet��$��24(�$	��101��$	��405��
���   �$y�Hidden=��� -#   $��showNameLabel��$ 55��$ 45��$��Name:��$��24(�
���   �$y�Hidden=��� -$   $��showGroupLabel��$ 55��$ 91��$��Group:��$��24(�
���   �$y�Hidden=��� -%   $��showTechnique��$ 55��$	 137��$��24(�
���   �$y�Hidden=�  -&   $��trainigButton��
�������$ 11��$	 745��$��Stop Training��$��24(�
���   �$��59��$��Center=�$��Center=�$	��172��$y�Hidden=��� -'   $��finishLabel��$��Thank You from Science��$	��200(�$	 215��$	 381��$��Gabriola)�$y�Hidden=��� -(   $��trainingLabel��$����Try to highlight some words for training. Use the button on your  bottom left (also with your finger) to activate highlight event.��$y�Hidden=�$��32(�
��� d �$	 465��$ 58��$	��203��$	��982��