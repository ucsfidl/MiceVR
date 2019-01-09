function varargout = mouseMapperGUI(varargin)
% MOUSEMAPPERGUI MATLAB code for mouseMapperGUI.fig
%      MOUSEMAPPERGUI, by itself, creates a new MOUSEMAPPERGUI or raises the existing
%      singleton*.
%
%      H = MOUSEMAPPERGUI returns the handle to a new MOUSEMAPPERGUI or the handle to
%      the existing singleton*.
%
%      MOUSEMAPPERGUI('CALLBACK',hObject,eventData,handles,...) calls the local
%      function named CALLBACK in MOUSEMAPPERGUI.M with the given input arguments.
%
%      MOUSEMAPPERGUI('Property','Value',...) creates a new MOUSEMAPPERGUI or raises the
%      existing singleton*.  Starting from the left, property value pairs are
%      applied to the GUI before mouseMapperGUI_OpeningFcn gets called.  An
%      unrecognized property name or invalid value makes property application
%      stop.  All inputs are passed to mouseMapperGUI_OpeningFcn via varargin.
%
%      *See GUI Options on GUIDE's Tools menu.  Choose "GUI allows only one
%      instance to run (singleton)".
%
% See also: GUIDE, GUIDATA, GUIHANDLES

% Edit the above text to modify the response to help mouseMapperGUI

% Last Modified by GUIDE v2.5 26-Jan-2017 16:10:31

% Begin initialization code - DO NOT EDIT
gui_Singleton = 1;
gui_State = struct('gui_Name',       mfilename, ...
                   'gui_Singleton',  gui_Singleton, ...
                   'gui_OpeningFcn', @mouseMapperGUI_OpeningFcn, ...
                   'gui_OutputFcn',  @mouseMapperGUI_OutputFcn, ...
                   'gui_LayoutFcn',  [] , ...
                   'gui_Callback',   []);
if nargin && ischar(varargin{1})
    gui_State.gui_Callback = str2func(varargin{1});
end

if nargout
    [varargout{1:nargout}] = gui_mainfcn(gui_State, varargin{:});
else
    gui_mainfcn(gui_State, varargin{:});
end
% End initialization code - DO NOT EDIT


% --- Executes just before mouseMapperGUI is made visible.
function mouseMapperGUI_OpeningFcn(hObject, eventdata, handles, varargin)
% This function has no output args, see OutputFcn.
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
% varargin   command line arguments to mouseMapperGUI (see VARARGIN)

% Choose default command line output for mouseMapperGUI
handles.output = hObject;
handles.replaySet = false;
handles.scenarioSet = false;

% Update handles structure
guidata(hObject, handles);

% UIWAIT makes mouseMapperGUI wait for user response (see UIRESUME)
% uiwait(handles.figure1);


% --- Outputs from this function are returned to the command line.
function varargout = mouseMapperGUI_OutputFcn(hObject, eventdata, handles) 
% varargout  cell array for returning output args (see VARARGOUT);
% hObject    handle to figure
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Get default command line output from handles structure
varargout{1} = handles.output;


% --- Executes on button press in setReplayButton.
function setReplayButton_Callback(hObject, eventdata, handles)
% hObject    handle to setReplayButton (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
[fileName, pathName] = uigetfile('*.*','Select file','MultiSelect','on');
cd(pathName);
handles.fileName = fileName;
handles.pathName = pathName;
replay = findobj('Tag', 'replayFileText');
if iscell(handles.fileName)
    set(replay,'String','Multiple');
else
    set(replay,'String',fileName);
end

guidata(hObject, handles);


% --- Executes on button press in setScenarioButton.
function setScenarioButton_Callback(hObject, eventdata, handles)
% hObject    handle to setScenarioButton (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
[fileName, pathName] = uigetfile('*.*','Select file');
cd(pathName);
xmlFile = xmlread(fileName);
doc = xmlFile.getElementsByTagName('document');
doc = doc.item(0);
trees=doc.getElementsByTagName('trees');
trees = trees.item(0);
t  = trees.getElementsByTagName('t');
walls=doc.getElementsByTagName('walls');
walls = walls.item(0);
w = walls.getElementsByTagName('wall');
handles.wpos = {[]};
handles.wrot = {[]};
handles.tpos = {[]};
handles.wsca = {[]};
for iter=0:w.getLength-1
    holder = w.item(iter).getElementsByTagName('pos').item(0).getFirstChild.getData;
    handles.wpos(iter+1,1:3) = textscan(char(holder),'%f;%f;%f');
    holder = w.item(iter).getElementsByTagName('rot').item(0).getFirstChild.getData;
    handles.wrot(iter+1,1:3) = textscan(char(holder),'%f;%f;%f');
    holder = w.item(iter).getElementsByTagName('scale').item(0).getFirstChild.getData;
    handles.wsca(iter+1,1:3) = textscan(char(holder),'%f;%f;%f');
end
for iter=0:t.getLength-1
    holder = t.item(iter).getElementsByTagName('pos').item(0).getFirstChild.getData;
    handles.tpos(iter+1,1:3) = textscan(char(holder),'%f;%f;%f');
end
scene = findobj('Tag', 'scenarioFileText');
set(scene,'String',fileName);

guidata(hObject,handles);

% --- Executes on slider movement.
function startSlider_Callback(hObject, eventdata, handles)
% hObject    handle to startSlider (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'Value') returns position of slider
%        get(hObject,'Min') and get(hObject,'Max') to determine range of slider
handles.startVal = get(hObject,'Value');
guidata(hObject, handles);


% --- Executes during object creation, after setting all properties.
function startSlider_CreateFcn(hObject, eventdata, handles)
% hObject    handle to startSlider (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: slider controls usually have a light gray background.
if isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor',[.9 .9 .9]);
end
set(hObject,'Value',0);
handles.startVal =0;
guidata(hObject, handles);



% --- Executes on slider movement.
function endSlider_Callback(hObject, eventdata, handles)
% hObject    handle to endSlider (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)

% Hints: get(hObject,'Value') returns position of slider
%        get(hObject,'Min') and get(hObject,'Max') to determine range of slider
handles.endVal = get(hObject,'Value');
guidata(hObject, handles);

% --- Executes during object creation, after setting all properties.
function endSlider_CreateFcn(hObject, eventdata, handles)
% hObject    handle to endSlider (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    empty - handles not created until after all CreateFcns called

% Hint: slider controls usually have a light gray background.
if isequal(get(hObject,'BackgroundColor'), get(0,'defaultUicontrolBackgroundColor'))
    set(hObject,'BackgroundColor',[.9 .9 .9]);
end
set(hObject,'Value',1);
handles.endVal =1;
guidata(hObject, handles);


% --- Executes on button press in generateButton.
function generateButton_Callback(hObject, eventdata, handles)
% hObject    handle to generateButton (see GCBO)
% eventdata  reserved - to be defined in a future version of MATLAB
% handles    structure with handles and user data (see GUIDATA)
cd(handles.pathName);
if iscell(handles.fileName)
   numFiles = size(handles.fileName,2); 
else
   numFiles =1;
end
for i = 0:numFiles-1
    if(numFiles>1) %Subplotting for multiple files
        if mod(i,8)==0 % At maximum 8 figures per image
           figure; 
        end
        subplot(2,4,mod(i,8)+1);
        fileID = fopen(handles.fileName{i+1});
        fN = handles.fileName{i+1};
    else
        fileID = fopen(handles.fileName);%Single File Case
        fN = handles.fileName;
        figure;
    end
    rewName = ['rew_',fN];
    %get replay points
    
    holderA = textscan(fileID, '%f,%f,%f;%f');
    holderB = holderA(1);
    xPos = holderB{1};
    holderB = holderA(3);
    zPos =holderB{1};
    holderB = holderA(4);
    euler =holderB{1};

    pose = [xPos(1:length(euler)), zPos(1:length(euler)), euler];
    clear holderA holderB xPos zPos euler

    %Find Outer limits
    xMin = min(pose(:,1));
    %xShift =xMin;
    xShift = pose(1,1);
    xMax = max(pose(:,1));
    zMin = min(pose(:,2));
    %zShift = zMin;
    zShift = pose(1,2);
    zMax = max(pose(:,2));
    numPts = size(pose,1);
    %Set to origin
    pose = pose -[xShift*ones(numPts,1), zShift*ones(numPts,1), zeros(numPts,1)];
    xMax = xMax-xShift;
    xMin = xMin-xShift;
    zMax = zMax-zShift;    
    zMin = zMin-zShift;
    

    %Percent Traveled
     poseDiff = diff(pose(:,1:2));
     vectDiff = zeros(length(poseDiff),1);
    for v = 1: length(poseDiff)
       vectDiff(v) = norm(poseDiff(v,1:2)); 
    end
    intFrames = 100;
    threshold = 2;
    vectSum = zeros(floor(length(vectDiff)/intFrames),1);
    for x = 1:length(vectDiff)/intFrames
        vectSum(x)= sum(vectDiff((x-1)*intFrames+1:(x-1)*intFrames+intFrames+1));
    end
    disp(fN);
    disp(['Percent Time Traveling Over' num2str(threshold) 'units per' num2str(intFrames) 'Frames:']);
    disp(num2str(length(find(vectSum>threshold))/length(vectSum)*100));
    
    startPt = floor(handles.startVal*numPts)+1;
    endPt = ceil(handles.endVal*numPts);
    hold on;
    border = 40;
    
    for iter = 0:length(handles.wrot)-1
        rotMat = [cosd(handles.wrot{iter+1,2}) sind(handles.wrot{iter+1,2}); ...
            -sind(handles.wrot{iter+1,2}) cosd(handles.wrot{iter+1,2})];
        x = handles.wpos{iter+1,1};
        z = handles.wpos{iter+1,3};
        %xScale = handles.wsca{iter+1,1}*2;
        %zScale = handles.wsca{iter+1,3}+50;
        xScale = handles.wsca{iter+1,1};
        zScale = handles.wsca{iter+1,3};
        vert = [-xScale/2,-zScale/2 ;xScale/2,-zScale/2;xScale/2,zScale/2 ;-xScale/2,zScale/2];
        rVert = rotMat*vert';
        patch(rVert(1,:)+x-xShift,rVert(2,:)+z-zShift,'red');
    end

    for iter=0:size(handles.tpos,1)-1
        viscircles([handles.tpos{iter+1,1}-xShift, handles.tpos{iter+1,3}-zShift],4.5);
    end

    if startPt<endPt
        plot(pose(startPt:endPt-3,1),pose(startPt:endPt-3,2),'-');
    end
    %Check if there's a reward file and plot
    if exist(rewName, 'file')
        testSize = dir(rewName);
        if testSize.bytes
            rewID = fopen(rewName);
            holderA = textscan(rewID, '%f;%f');
            holderB = holderA(1);
            rewards = holderB{1};
            holderB = holderA(2);
            licks =holderB{1};
            
            lickDiff = diff(licks);
            ind = find(lickDiff~=0);
            plot(pose(ind,1),pose(ind,2),'rx');
        end
    end
    
    %xlim([xMin-border,xMax+border]);   %Center around trace
    %ylim([zMin-border,zMax+border]);
    xlim([-30,30]);
    ylim([-20,60]);
    axis equal;
    title(fN);
end