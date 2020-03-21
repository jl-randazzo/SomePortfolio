#include "mainwindow.h"
#include "ui_mainwindow.h"
#include <framelistview.h>
#include <QFileDialog>
#include <QDropEvent>
#include <QMimeData>
#include <QDebug>

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
{
    ui->setupUi(this);
    this->setCentralWidget(ui->primaryWidget);
    ui->primaryView->setAcceptDrops(true);
    ui->frameListView->initialize();
    ui->frameListView->show();

    updateTimer = new QTimer(this);
    updateTimer->setInterval(1000);
    connect(updateTimer, SIGNAL(timeout()), this, SLOT(update()));
    updateTimer->start();

    connect(ui->frameListView, SIGNAL(onSelectionChanged()), this, SLOT(update()));
    connect(ui->frameListView, &FrameListView::onSelectionChanged, this, [=](){ qInfo() << "onSelectionChanged signal caught";});
}

MainWindow::~MainWindow()
{
    delete updateTimer;
    delete ui;
}

void MainWindow::update(){
    ui->primaryView->update();
}

void MainWindow::on_actionImport_Sprites_triggered() {
    QFileDialog dialog;
    QUrl url = dialog.getOpenFileUrl();
    qInfo() << "Test: " << url;
}

void MainWindow::on_actionUndo_triggered()
{
    qInfo() << "Undo event triggered";
}

void MainWindow::on_genGrayScale_clicked()
{
    AnimationSessionBuilder::getInstance()->generateGrayScaleImages();
}

void MainWindow::on_displayOriginal_clicked()
{
    ui->primaryView->setDisplayMode(Sprite);
    update();
}

void MainWindow::on_displayGray_clicked()
{
    ui->primaryView->setDisplayMode(GrayScale);
    update();
}


void MainWindow::on_displayLines_clicked()
{
    ui->primaryView->setDisplayMode(Lines);
    update();
}

void MainWindow::on_displaySobel_clicked()
{
    ui->primaryView->setDisplayMode(Edges);
    update();
}

void MainWindow::on_displayErosion_clicked()
{
    ui->primaryView->setDisplayMode(Eroded);
    update();
}

void MainWindow::on_displayDepop_clicked()
{
    ui->primaryView->setDisplayMode(Depop);
    update();
}

void MainWindow::on_displayCandidates_clicked()
{
   ui->primaryView->setDisplayMode(Candidates);
   update();
}
