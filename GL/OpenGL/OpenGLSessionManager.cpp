#include<OpenGLSessionManager.h>


// general graphics utilities

int getSizeForGLenumType(GLenum type) {
    switch(type){
    case GL_FLOAT:
        return 4;
    default:
        return 4;
    }
}

// OpenGLSessionManager

OpenGLSessionManager* OpenGLSessionManager::instance;

OpenGLSessionManager* OpenGLSessionManager::getInstance(){
    if(instance == nullptr){
        throw ER_CONTEXT_DOES_NOT_EXIST;
    }
    return instance;
}

OpenGLSessionManager::OpenGLSessionManager(QOpenGLContext const* context) : context(context) {
    qInfo() << "Context initialized successfully: " << context->isValid();
    shader_map = new std::map<QString, ProgramShaderHandles>();
    loadShaderPrograms();
}

void OpenGLSessionManager::buildSession(QOpenGLContext* context){
    if(instance != nullptr)
        throw ER_CONTEXT_ALREADY_EXISTS;

    instance = new OpenGLSessionManager(context);
}

const QOpenGLContext * OpenGLSessionManager::getContext() const {
    return context;
}

GLuint OpenGLSessionManager::useProgram(QString name) const {
    ProgramShaderHandles handles = shader_map->at(name);

    context->functions()->glLinkProgram(handles.prog_handle);
    GLint is_linked = 0;
    context->functions()->glGetProgramiv(handles.prog_handle, GL_LINK_STATUS, &is_linked);

    // log link errors
    if(is_linked == GL_FALSE){
        logProgramLinkError(name, handles.prog_handle);
    }

    context->functions()->glUseProgram(handles.prog_handle);
    //qInfo() << "Error post use program " << name << ": " << context->functions()->glGetError();

    GLint param = 0;
    context->functions()->glValidateProgram(handles.prog_handle);
    context->functions()->glGetProgramiv(handles.prog_handle, GL_VALIDATE_STATUS, &param);
    if(param == GL_FALSE) throw -1; // fail hard if program is invalid

    return handles.prog_handle;
}

void OpenGLSessionManager::setUniform2DTextureValue(GLuint prog_id, const char * name, GLuint tex_id, GLint index, GLenum tex_enum) const{
    GLint grayscale_loc = context->functions()->glGetUniformLocation(prog_id, name);
    //qInfo() << "Error post get uniform " << name << ": " << context->functions()->glGetError();

    context->functions()->glUniform1i(grayscale_loc, index);
    //qInfo() << "Error post set uniform " << name << ": " << context->functions()->glGetError();

    context->functions()->glActiveTexture(tex_enum);

    context->functions()->glBindTexture(GL_TEXTURE_2D, tex_id);
    //qInfo() << "Error post bind texture " << name << ": " <<context->functions()->glGetError();
}

void OpenGLSessionManager::configureAttributesAndDraw
(const GLenum draw_type, size_t attribute_count, const int * attribute_sizes, const GLenum * types, size_t vertex_count, const void * data) const
{
    int byte_count = 0;
    for(int i = 0; i < attribute_count; i++){
        byte_count += getSizeForGLenumType(types[i]) * attribute_sizes[i];
    }

    GLuint buffer = 0;
    context->functions()->glGenBuffers(1, &buffer);
    context->functions()->glBindBuffer(GL_ARRAY_BUFFER, buffer);
    context->functions()->glBufferData(GL_ARRAY_BUFFER, byte_count * vertex_count, data, GL_STATIC_DRAW);

    int running_sum = 0;
    for(int i = 0; i < attribute_count; i++){
        context->functions()->glEnableVertexAttribArray(i);
        context->functions()->glVertexAttribPointer(i, attribute_sizes[i], types[i], GL_FALSE, byte_count, (void*)running_sum);
        //qInfo() << "Error post pointer " << (context->functions()->glGetError() == GL_INVALID_VALUE);

        running_sum += getSizeForGLenumType(types[i]) * attribute_sizes[i];
    }

    context->functions()->glDrawArrays(draw_type, 0, vertex_count);
    //qInfo() << "Error post draw " << context->functions()->glGetError();

    for(int i = 1; i < attribute_count; i++){
        context->functions()->glDisableVertexAttribArray(i);
    }
}


void OpenGLSessionManager::loadShaderPrograms() {
    std::vector<VertFragShaderData> shader_data;
    shader_data.push_back(
                VertFragShaderData("Default",
                NamePathPair("DefaultVertexShader", ":/Shaders/DefaultVertex.glsl"),
                NamePathPair("DefaultFragmentShader", ":/Shaders/DefaultFragment.glsl")));
    shader_data.push_back(
                VertFragShaderData("Grayscale",
                NamePathPair("GrayscaleVertexShader", ":/Shaders/GrayscaleVertex.glsl"),
                NamePathPair("GrayscaleFragmentShader", ":/Shaders/GrayscaleFragment.glsl")));

    for(auto data : shader_data){
        GLuint prog_id = context->functions()->glCreateProgram();
        qInfo() << "Error post create program: " << context->functions()->glGetError();

        GLuint vert_shader_handle = 0, frag_shader_handle = 0;
        if(!data.vert.path.isEmpty()){
            vert_shader_handle = getCompiledShaderHandle(data.vert, GL_VERTEX_SHADER);
            context->functions()->glAttachShader(prog_id, vert_shader_handle);
        }

        if(!data.frag.path.isEmpty()){
            frag_shader_handle = getCompiledShaderHandle(data.frag, GL_FRAGMENT_SHADER);
            context->functions()->glAttachShader(prog_id, frag_shader_handle);
        }

        std::pair<QString, ProgramShaderHandles> pair(data.name, ProgramShaderHandles(prog_id, vert_shader_handle, frag_shader_handle));
        qInfo() << "element added " << shader_map->insert(pair).second;
    }
}

GLuint OpenGLSessionManager::getCompiledShaderHandle(NamePathPair name_path_pair, GLenum shader_type) const {
    QFile shader_file(name_path_pair.path);
    shader_file.open(QIODevice::ReadOnly);
    const char * shader_code_array[1];
    const std::string shader_code = QString(shader_file.readAll()).toStdString();
    shader_code_array[0] = shader_code.data();

    //const std::string shader_code = QString(shader_file.readAll()).toStdString();
    shader_file.close();

    GLuint shader_handle = context->functions()->glCreateShader(shader_type);
    context->functions()->glShaderSource(shader_handle, 1, shader_code_array, nullptr);
    context->functions()->glCompileShader(shader_handle);
    qInfo() << "Error post compile shader for " << name_path_pair.name << ": " << context->functions()->glGetError();

    GLint is_compiled = 0;
    context->functions()->glGetShaderiv(shader_handle, GL_COMPILE_STATUS, &is_compiled);
    qInfo() << "Is compiled for " << name_path_pair.name << ": "  << (is_compiled != GL_FALSE);

    if(is_compiled == GL_FALSE){
        logShaderCompilationError(name_path_pair.name, shader_handle);
    }

    return shader_handle;
}

QOpenGLTexture const* OpenGLSessionManager::createOpenGLTexture(const QImage *image){
    QOpenGLTexture *texture = new QOpenGLTexture(image->mirrored(), QOpenGLTexture::MipMapGeneration::DontGenerateMipMaps);
    texture->create();
    return texture;
}

void OpenGLSessionManager::logShaderCompilationError(QString shader_name, GLuint shader_handle) const{
    GLint length = 0;
    context->functions()->glGetShaderiv(shader_handle, GL_INFO_LOG_LENGTH, &length);
    char* compiler_error = new char[static_cast<uint>(length)];
    context->functions()->glGetShaderInfoLog(shader_handle, length, &length, compiler_error);
    qInfo() << "Compiler error  for " << shader_name << ": " << QString(compiler_error);
    delete[] compiler_error;
    context->functions()->glDeleteShader(shader_handle);
}

void OpenGLSessionManager::logProgramLinkError(QString prog_name, GLuint prog_handle) const {
    GLint param = 0;
    context->functions()->glGetProgramiv(prog_handle, GL_INFO_LOG_LENGTH, &param);
    char* log = new char[param];
    context->functions()->glGetProgramInfoLog(prog_handle, param, &param, log);
    qInfo() << "Linker errors for program " << prog_name << ": \n" << log;
    delete[] log;
}
